using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Application.Events;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Infrastructure.Locks;

namespace TicketSystem.Infrastructure.Services
{
    public class ReservationService : BaseService<Reservation>, IReservationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<Trip> _tripRepository;
        private readonly IRepository<Seat> _seatRepository;
        private readonly IRepository<Passenger> _passengerRepository;
        private readonly IRepository<ReservationSeat> _reservationSeatRepository;
        private readonly IPassengerService _passengerService;
        private readonly IDistributedLockService _lockService;
        private readonly IMapper _mapper;
        private readonly ILogger<ReservationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private const int EXPIRATION_MINUTES = 15;

        public ReservationService(
        IRepository<Reservation> reservationRepository,
        ApplicationDbContext context,
        IRepository<Trip> tripRepository,
        IRepository<Seat> seatRepository,
        IRepository<Passenger> passengerRepository,
        IRepository<ReservationSeat> reservationSeatRepository,
        IPassengerService passengerService,
        IDistributedLockService lockService,
        IMapper mapper,
        ILogger<ReservationService> logger,
        IServiceProvider serviceProvider)
        : base(reservationRepository)
        {
            _context = context;
            _tripRepository = tripRepository;
            _seatRepository = seatRepository;
            _passengerRepository = passengerRepository;
            _reservationSeatRepository = reservationSeatRepository;
            _passengerService = passengerService;
            _lockService = lockService;
            _mapper = mapper;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<ReservationDto> CreateReservationAsync(CreateReservationDto createDto)
        {
            _logger.LogInformation("Iniciando criacao de reserva para TripId: {TripId}, SeatNumbers: {SeatNumbers}",
            createDto.TripId, string.Join(", ", createDto.SeatNumbers));

            var lockKey = LockKeys.GetTripReservationKey(createDto.TripId);
            _logger.LogDebug("Tentando adquirir lock para viagem: {TripId}", createDto.TripId);

            using var lockHandle = await _lockService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10));

            _logger.LogDebug("Lock adquirido para viagem: {TripId}", createDto.TripId);

            var trip = await _tripRepository.GetByIdAsync(createDto.TripId);
            if (trip == null)
            {
                _logger.LogWarning("Viagem nao encontrada: {TripId}", createDto.TripId);
                throw new KeyNotFoundException("Viagem com ID " + createDto.TripId + " nao encontrada");
            }

            if (trip.Status == TripStatus.Completed || trip.Status == TripStatus.Cancelled)
            {
                _logger.LogWarning("Tentativa de reserva em viagem finalizada/cancelada. TripId: {TripId}, Status: {Status}",
                createDto.TripId, trip.Status);
                throw new InvalidOperationException("Nao e possivel reservar assentos para uma viagem finalizada ou cancelada");
            }

            if (trip.DepartureTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Tentativa de reserva em viagem que ja partiu. TripId: {TripId}, DepartureTime: {DepartureTime}",
                createDto.TripId, trip.DepartureTime);
                throw new InvalidOperationException("Nao e possivel reservar assentos para uma viagem que ja partiu");
            }

            var passenger = await _passengerService.GetOrCreatePassengerAsync(createDto.Passenger);
            _logger.LogDebug("Passageiro obtido/criado. PassengerId: {PassengerId}, Document: {Document}",
            passenger.Id, passenger.Document);

            var hasPending = await _passengerService.HasPendingReservationForTripAsync(passenger.Document, trip.Id);
            if (hasPending)
            {
                _logger.LogWarning("Passageiro ja possui reserva pendente. PassengerId: {PassengerId}, TripId: {TripId}",
                passenger.Id, trip.Id);
                throw new InvalidOperationException("Voce ja possui uma reserva pendente para esta viagem");
            }

            var seats = new List<Seat>();
            var seatNumbers = new List<string>();

            foreach (var seatNumber in createDto.SeatNumbers)
            {
                _logger.LogDebug("Verificando assento: {SeatNumber}", seatNumber);

                var seatList = await _seatRepository.FindAsync(s =>
                s.TripId == trip.Id &&
                s.Number == seatNumber &&
                s.IsActive);

                var seat = seatList.FirstOrDefault();
                if (seat == null)
                {
                    _logger.LogWarning("Assento nao encontrado. TripId: {TripId}, SeatNumber: {SeatNumber}",
                    trip.Id, seatNumber);
                    throw new KeyNotFoundException("Assento " + seatNumber + " nao encontrado nesta viagem");
                }

                if (seat.Status != SeatStatus.Available)
                {
                    _logger.LogWarning("Assento nao esta disponivel. TripId: {TripId}, SeatNumber: {SeatNumber}, Status: {Status}",
                    trip.Id, seatNumber, seat.Status);
                    throw new InvalidOperationException("Assento " + seatNumber + " nao esta disponivel");
                }

                seats.Add(seat);
                seatNumbers.Add(seatNumber);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    _logger.LogDebug("Transacao iniciada para TripId: {TripId}", createDto.TripId);

                    var totalAmount = seats.Sum(s => trip.Price * (s.PriceMultiplier ?? 1.0m));
                    _logger.LogDebug("Total da reserva: {TotalAmount}", totalAmount);

                    var reservation = new Reservation
                    {
                        TripId = trip.Id,
                        PassengerId = passenger.Id,
                        ReservationDate = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(EXPIRATION_MINUTES),
                        Status = ReservationStatus.Pending,
                        TotalAmount = totalAmount,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdReservation = await _repository.AddAsync(reservation);
                    _logger.LogInformation("Reserva criada. ReservationId: {ReservationId}", createdReservation.Id);

                    foreach (var seat in seats)
                    {
                        seat.Status = SeatStatus.Reserved;
                        seat.PassengerName = passenger.Name;
                        seat.PassengerDocument = passenger.Document;
                        seat.UpdatedAt = DateTime.UtcNow;
                        await _seatRepository.UpdateAsync(seat);

                        var reservationSeat = new ReservationSeat
                        {
                            ReservationId = createdReservation.Id,
                            SeatId = seat.Id,
                            Price = trip.Price * (seat.PriceMultiplier ?? 1.0m),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _reservationSeatRepository.AddAsync(reservationSeat);
                        _logger.LogDebug("Assento associado a reserva. SeatId: {SeatId}, Number: {Number}", seat.Id, seat.Number);
                    }

                    await transaction.CommitAsync();
                    _logger.LogInformation("Transacao confirmada. ReservationId: {ReservationId}", createdReservation.Id);

                    try
                    {
                        var eventPublisher = _serviceProvider.GetRequiredService<IEventPublisher>();

                        var createdEvent = new ReservationCreatedEvent
                        {
                            ReservationId = createdReservation.Id,
                            TripId = trip.Id,
                            PassengerId = passenger.Id,
                            PassengerName = passenger.Name,
                            PassengerEmail = passenger.Email,
                            PassengerDocument = passenger.Document,
                            SeatNumbers = seatNumbers,
                            TotalAmount = totalAmount,
                            ReservationDate = reservation.ReservationDate,
                            ExpiresAt = reservation.ExpiresAt,
                            TripOrigin = trip.Origin,
                            TripDestination = trip.Destination,
                            TripDepartureTime = trip.DepartureTime,
                            CreatedAt = DateTime.UtcNow
                        };

                        await eventPublisher.PublishAsync(createdEvent);
                        _logger.LogInformation("Evento ReservationCreatedEvent publicado. ReservationId: {ReservationId}", createdReservation.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao publicar evento ReservationCreatedEvent. ReservationId: {ReservationId}", createdReservation.Id);
                    }

                    return await GetReservationByIdAsync(createdReservation.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar reserva. TripId: {TripId}", createDto.TripId);
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<ReservationDto> GetReservationByIdAsync(Guid id)
        {
            var reservation = await _context.Reservations
            .Include(r => r.Trip)
            .Include(r => r.Passenger)
            .Include(r => r.ReservationSeats)
            .ThenInclude(rs => rs.Seat)
            .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (reservation == null)
                throw new KeyNotFoundException($"Reserva com ID {id} nao encontrada");

            return MapToDto(reservation);
        }

        public async Task<ReservationDto> ConfirmReservationAsync(ConfirmReservationDto confirmDto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var reservation = await _context.Reservations
                    .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                    .Include(r => r.Trip)
                    .Include(r => r.Passenger)
                    .FirstOrDefaultAsync(r => r.Id == confirmDto.ReservationId && r.IsActive);

                    if (reservation == null)
                        throw new KeyNotFoundException($"Reserva com ID {confirmDto.ReservationId} nao encontrada");

                    if (reservation.Status != ReservationStatus.Pending)
                        throw new InvalidOperationException($"Reserva esta com status {reservation.Status} e nao pode ser confirmada");

                    if (reservation.ExpiresAt < DateTime.UtcNow)
                        throw new InvalidOperationException("Reserva expirou. Por favor, faca uma nova reserva.");

                    if (reservation.Trip.Status == TripStatus.Completed || reservation.Trip.Status == TripStatus.Cancelled)
                        throw new InvalidOperationException("Nao e possivel confirmar reserva para uma viagem finalizada ou cancelada");

                    foreach (var reservationSeat in reservation.ReservationSeats)
                    {
                        var seat = reservationSeat.Seat;
                        if (seat.Status != SeatStatus.Reserved)
                        {
                            throw new InvalidOperationException($"Assento {seat.Number} nao esta mais disponivel");
                        }

                        seat.Status = SeatStatus.Sold;
                        seat.UpdatedAt = DateTime.UtcNow;
                        await _seatRepository.UpdateAsync(seat);
                    }

                    reservation.Status = ReservationStatus.Confirmed;
                    reservation.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(reservation);

                    await transaction.CommitAsync();

                    // ============================================
                    // PUBLICAR EVENTO DE RESERVA CONFIRMADA
                    // ============================================
                    try
                    {
                        var eventPublisher = _serviceProvider.GetRequiredService<IEventPublisher>();

                        var confirmedEvent = new ReservationConfirmedEvent
                        {
                            ReservationId = reservation.Id,
                            TripId = reservation.TripId,
                            PassengerId = reservation.PassengerId,
                            PassengerName = reservation.Passenger?.Name ?? string.Empty,
                            PassengerEmail = reservation.Passenger?.Email ?? string.Empty,
                            PassengerDocument = reservation.Passenger?.Document ?? string.Empty,
                            Seats = reservation.ReservationSeats.Select(rs => new SeatInfo
                            {
                                Number = rs.Seat?.Number ?? string.Empty,
                                Type = rs.Seat?.Type.ToString() ?? string.Empty,
                                Price = rs.Price,
                                Row = rs.Seat?.Row ?? 0,
                                Column = rs.Seat?.Column ?? 0
                            }).ToList(),
                            TotalAmount = reservation.TotalAmount,
                            ConfirmedAt = DateTime.UtcNow,
                            PaymentMethod = confirmDto.PaymentMethod,
                            TripOrigin = reservation.Trip?.Origin ?? string.Empty,
                            TripDestination = reservation.Trip?.Destination ?? string.Empty,
                            TripDepartureTime = reservation.Trip?.DepartureTime ?? DateTime.MinValue,
                            TicketCode = GenerateTicketCode(reservation)
                        };

                        await eventPublisher.PublishAsync(confirmedEvent);
                        _logger.LogInformation("ReservationConfirmedEvent publicado. ReservationId: {ReservationId}", reservation.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao publicar ReservationConfirmedEvent. ReservationId: {ReservationId}", reservation.Id);
                    }

                    return await GetReservationByIdAsync(reservation.Id);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private string GenerateTicketCode(Reservation reservation)
        {
            var now = DateTime.UtcNow;
            var prefix = "TKT";
            var date = now.ToString("yyMMdd");
            var random = new Random().Next(1000, 9999);
            var hash = reservation.Id.ToString().Substring(0, 6);
            return prefix + "-" + date + "-" + random + "-" + hash;
        }

        public async Task CancelReservationAsync(Guid reservationId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var reservation = await _context.Reservations
                    .Include(r => r.ReservationSeats)
                    .ThenInclude(rs => rs.Seat)
                    .FirstOrDefaultAsync(r => r.Id == reservationId && r.IsActive);

                    if (reservation == null)
                        throw new KeyNotFoundException($"Reserva com ID {reservationId} nao encontrada");

                    if (reservation.Status == ReservationStatus.Cancelled)
                        throw new InvalidOperationException("Reserva ja esta cancelada");

                    if (reservation.Status == ReservationStatus.Confirmed)
                        throw new InvalidOperationException("Reserva confirmada nao pode ser cancelada");

                    if (reservation.Status == ReservationStatus.Expired)
                        throw new InvalidOperationException("Reserva ja expirou");

                    foreach (var reservationSeat in reservation.ReservationSeats)
                    {
                        var seat = reservationSeat.Seat;
                        if (seat.Status == SeatStatus.Reserved)
                        {
                            seat.Status = SeatStatus.Available;
                            seat.PassengerName = null;
                            seat.PassengerDocument = null;
                            seat.UpdatedAt = DateTime.UtcNow;
                            await _seatRepository.UpdateAsync(seat);
                        }
                    }

                    reservation.Status = ReservationStatus.Cancelled;
                    reservation.IsActive = false;
                    reservation.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(reservation);

                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<AvailableSeatsDto> GetAvailableSeatsAsync(Guid tripId)
        {
            var trip = await _context.Trips
            .Include(t => t.Bus)
            .FirstOrDefaultAsync(t => t.Id == tripId && t.IsActive);

            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {tripId} nao encontrada");

            var seats = await _context.Seats
            .Where(s => s.TripId == tripId && s.IsActive)
            .OrderBy(s => s.Row)
            .ThenBy(s => s.Column)
            .ToListAsync();

            var availableSeats = seats.Where(s => s.Status == SeatStatus.Available).ToList();

            var dto = new AvailableSeatsDto
            {
                TripId = trip.Id,
                TripOrigin = trip.Origin,
                TripDestination = trip.Destination,
                TripDepartureTime = trip.DepartureTime,
                TotalSeats = seats.Count,
                AvailableSeats = availableSeats.Count,
                ReservedSeats = seats.Count(s => s.Status == SeatStatus.Reserved),
                SoldSeats = seats.Count(s => s.Status == SeatStatus.Sold),
                Seats = _mapper.Map<List<SeatDto>>(seats)
            };

            return dto;
        }

        public async Task<IEnumerable<ReservationDto>> GetReservationsByTripIdAsync(Guid tripId)
        {
            var reservations = await _context.Reservations
            .Include(r => r.Trip)
            .Include(r => r.Passenger)
            .Include(r => r.ReservationSeats)
            .ThenInclude(rs => rs.Seat)
            .Where(r => r.TripId == tripId && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

            return reservations.Select(MapToDto);
        }

        public async Task<IEnumerable<ReservationDto>> GetReservationsByPassengerDocumentAsync(string document)
        {
            var passenger = await _passengerRepository.FindAsync(p => p.Document == document);
            var passengerEntity = passenger.FirstOrDefault();

            if (passengerEntity == null)
                return new List<ReservationDto>();

            var reservations = await _context.Reservations
            .Include(r => r.Trip)
            .Include(r => r.Passenger)
            .Include(r => r.ReservationSeats)
            .ThenInclude(rs => rs.Seat)
            .Where(r => r.PassengerId == passengerEntity.Id && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

            return reservations.Select(MapToDto);
        }

        public async Task ExpirePendingReservationsAsync()
        {
            var now = DateTime.UtcNow;

            var expiredReservations = await _context.Reservations
            .Include(r => r.ReservationSeats)
            .ThenInclude(rs => rs.Seat)
            .Where(r => r.Status == ReservationStatus.Pending &&
            r.ExpiresAt < now &&
            r.IsActive)
            .ToListAsync();

            foreach (var reservation in expiredReservations)
            {
                foreach (var reservationSeat in reservation.ReservationSeats)
                {
                    var seat = reservationSeat.Seat;
                    if (seat.Status == SeatStatus.Reserved)
                    {
                        seat.Status = SeatStatus.Available;
                        seat.PassengerName = null;
                        seat.PassengerDocument = null;
                        seat.UpdatedAt = DateTime.UtcNow;
                    }
                }

                reservation.Status = ReservationStatus.Expired;
                reservation.IsActive = false;
                reservation.UpdatedAt = now;
            }

            if (expiredReservations.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ExpiredReservationDto>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var expiredReservations = await _context.Reservations
            .Include(r => r.Passenger)
            .Include(r => r.ReservationSeats)
            .ThenInclude(rs => rs.Seat)
            .Where(r => r.Status == ReservationStatus.Pending &&
            r.ExpiresAt < now &&
            r.IsActive)
            .ToListAsync(cancellationToken);

            return expiredReservations.Select(r => new ExpiredReservationDto
            {
                Id = r.Id,
                TripId = r.TripId,
                PassengerId = r.PassengerId,
                PassengerName = r.Passenger?.Name ?? string.Empty,
                PassengerEmail = r.Passenger?.Email ?? string.Empty,
                ExpiresAt = r.ExpiresAt,
                Seats = r.ReservationSeats.Select(rs => new ExpiredSeatInfo
                {
                    SeatId = rs.SeatId,
                    SeatNumber = rs.Seat?.Number ?? string.Empty
                }).ToList()
            });
        }

        public async Task<IEnumerable<ReservationDto>> GetAllReservationsAsync()
        {
            var reservations = await _context.Reservations
            .Include(r => r.Trip)
            .Include(r => r.Passenger)
            .Include(r => r.ReservationSeats)
            .ThenInclude(rs => rs.Seat)
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

            return reservations.Select(MapToDto);
        }

        private ReservationDto MapToDto(Reservation reservation)
        {
            var dto = new ReservationDto
            {
                Id = reservation.Id,
                TripId = reservation.TripId,
                TripOrigin = reservation.Trip?.Origin ?? string.Empty,
                TripDestination = reservation.Trip?.Destination ?? string.Empty,
                TripDepartureTime = reservation.Trip?.DepartureTime ?? DateTime.MinValue,
                PassengerId = reservation.PassengerId,
                PassengerName = reservation.Passenger?.Name ?? string.Empty,
                PassengerDocument = reservation.Passenger?.Document ?? string.Empty,
                PassengerEmail = reservation.Passenger?.Email ?? string.Empty,
                PassengerPhone = reservation.Passenger?.Phone ?? string.Empty,
                ReservationDate = reservation.ReservationDate,
                ExpiresAt = reservation.ExpiresAt,
                Status = reservation.Status,
                TotalAmount = reservation.TotalAmount,
                CreatedAt = reservation.CreatedAt,
                UpdatedAt = reservation.UpdatedAt,
                IsActive = reservation.IsActive,
                Seats = reservation.ReservationSeats.Select(rs => new ReservationSeatDto
                {
                    Id = rs.Id,
                    SeatId = rs.SeatId,
                    SeatNumber = rs.Seat?.Number ?? string.Empty,
                    SeatType = rs.Seat?.Type ?? SeatType.Middle,
                    Row = rs.Seat?.Row ?? 0,
                    Column = rs.Seat?.Column ?? 0,
                    Price = rs.Price
                }).ToList()
            };

            return dto;
        }
    }
}
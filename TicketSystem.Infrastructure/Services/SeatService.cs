using AutoMapper;
using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Infrastructure.Services
{
    public class SeatService : BaseService<Seat>, ISeatService
    {
        private readonly IRepository<Trip> _tripRepository;
        private readonly IMapper _mapper;

        public SeatService(
        IRepository<Seat> seatRepository,
        IRepository<Trip> tripRepository,
        IMapper mapper)
        : base(seatRepository)
        {
            _tripRepository = tripRepository;
            _mapper = mapper;
        }

        public async Task<SeatDto> GetSeatByIdAsync(Guid id)
        {
            var seat = await _repository.GetByIdAsync(id);

            if (seat == null)
                throw new KeyNotFoundException($"Assento com ID {id} não encontrado");

            return _mapper.Map<SeatDto>(seat);
        }

        public async Task<IEnumerable<SeatDto>> GetSeatsByTripIdAsync(Guid tripId)
        {
            var tripExists = await _tripRepository.ExistsAsync(t => t.Id == tripId);
            if (!tripExists)
                throw new KeyNotFoundException($"Viagem com ID {tripId} não encontrada");

            var seats = await _repository.FindAsync(s => s.TripId == tripId && s.IsActive);
            return _mapper.Map<IEnumerable<SeatDto>>(seats.OrderBy(s => s.Row).ThenBy(s => s.Column));
        }

        public async Task<SeatDto> CreateSeatAsync(CreateSeatDto createSeatDto)
        {
            var tripExists = await _tripRepository.ExistsAsync(t => t.Id == createSeatDto.TripId);
            if (!tripExists)
                throw new KeyNotFoundException($"Viagem com ID {createSeatDto.TripId} não encontrada");

            var exists = await _repository.ExistsAsync(s => s.TripId == createSeatDto.TripId && s.Number == createSeatDto.Number);
            if (exists)
                throw new InvalidOperationException($"Assento {createSeatDto.Number} já existe nesta viagem");

            var seat = _mapper.Map<Seat>(createSeatDto);
            var createdSeat = await _repository.AddAsync(seat);

            return _mapper.Map<SeatDto>(createdSeat);
        }

        public async Task<SeatDto> UpdateSeatAsync(UpdateSeatDto updateSeatDto)
        {
            var seat = await _repository.GetByIdAsync(updateSeatDto.Id);
            if (seat == null)
                throw new KeyNotFoundException($"Assento com ID {updateSeatDto.Id} não encontrado");

            var tripExists = await _tripRepository.ExistsAsync(t => t.Id == updateSeatDto.TripId);
            if (!tripExists)
                throw new KeyNotFoundException($"Viagem com ID {updateSeatDto.TripId} não encontrada");

            var exists = await _repository.ExistsAsync(s => s.TripId == updateSeatDto.TripId && s.Number == updateSeatDto.Number && s.Id != updateSeatDto.Id);
            if (exists)
                throw new InvalidOperationException($"Assento {updateSeatDto.Number} já existe nesta viagem");

            _mapper.Map(updateSeatDto, seat);
            await _repository.UpdateAsync(seat);

            return _mapper.Map<SeatDto>(seat);
        }

        public async Task DeleteSeatAsync(Guid id)
        {
            var seat = await _repository.GetByIdAsync(id);
            if (seat == null)
                throw new KeyNotFoundException($"Assento com ID {id} não encontrado");

            seat.IsActive = false;
            await _repository.UpdateAsync(seat);
        }

        public async Task<SeatDto> UpdateSeatStatusAsync(Guid seatId, UpdateSeatStatusDto updateDto)
        {
            var seat = await _repository.GetByIdAsync(seatId);
            if (seat == null)
                throw new KeyNotFoundException($"Assento com ID {seatId} não encontrado");

            var trip = await _tripRepository.GetByIdAsync(seat.TripId);
            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {seat.TripId} não encontrada");

            if (trip.Status == TripStatus.Completed)
                throw new InvalidOperationException("Não é possível alterar assento de uma viagem já concluída");

            if (trip.Status == TripStatus.Cancelled)
                throw new InvalidOperationException("Não é possível alterar assento de uma viagem já cancelada");

            if (seat.Status == SeatStatus.Sold && updateDto.Status != SeatStatus.Sold)
                throw new InvalidOperationException("Não é possível alterar um assento já vendido");

            seat.Status = updateDto.Status;
            seat.PassengerName = updateDto.PassengerName;
            seat.PassengerDocument = updateDto.PassengerDocument;

            await _repository.UpdateAsync(seat);

            return _mapper.Map<SeatDto>(seat);
        }

        public async Task<IEnumerable<SeatDto>> GenerateSeatsForTripAsync(Guid tripId, int capacity)
        {
            var trip = await _tripRepository.GetByIdAsync(tripId);
            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {tripId} não encontrada");

            // Verifica se já existem assentos para esta viagem
            var existingSeats = await _repository.FindAsync(s => s.TripId == tripId);
            if (existingSeats.Any())
                throw new InvalidOperationException("Esta viagem já possui assentos gerados");

            var seats = new List<Seat>();
            var columns = 4; // A, B, C, D
            var rows = (int)Math.Ceiling((double)capacity / columns);

            var seatNumber = 1;

            for (int row = 1; row <= rows; row++)
            {
                for (int col = 1; col <= columns; col++)
                {
                    if (seats.Count >= capacity)
                        break;

                    var columnLetter = (char)('A' + col - 1);
                    var number = $"{row}{columnLetter}";

                    var seatType = col == 1 || col == columns ? SeatType.Window :
                    col == 2 ? SeatType.Aisle : SeatType.Middle;

                    var priceMultiplier = 1.0m;
                    if (seatType == SeatType.Window)
                        priceMultiplier = 1.1m;
                    else if (seatType == SeatType.Aisle)
                        priceMultiplier = 1.05m;

                    seats.Add(new Seat
                    {
                        TripId = tripId,
                        Number = number,
                        Type = seatType,
                        Status = SeatStatus.Available,
                        Row = row,
                        Column = col,
                        PriceMultiplier = priceMultiplier,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            foreach (var seat in seats)
            {
                await _repository.AddAsync(seat);
            }

            return _mapper.Map<IEnumerable<SeatDto>>(seats);
        }

        public async Task<bool> IsSeatNumberAvailableAsync(Guid tripId, string seatNumber)
        {
            return !await _repository.ExistsAsync(s => s.TripId == tripId && s.Number == seatNumber);
        }

        public async Task<bool> IsSeatNumberAvailableAsync(Guid tripId, string seatNumber, Guid excludeId)
        {
            return !await _repository.ExistsAsync(s => s.TripId == tripId && s.Number == seatNumber && s.Id != excludeId);
        }
    }
}
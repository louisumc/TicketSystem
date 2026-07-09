using AutoMapper;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Infrastructure.Services
{
    public class TripService : BaseService<Trip>, ITripService
    {
        private readonly IRepository<Bus> _busRepository;
        private readonly IRepository<Seat> _seatRepository;
        private readonly ISeatService _seatService;
        private readonly IMapper _mapper;

        public TripService(
        IRepository<Trip> tripRepository,
        IRepository<Bus> busRepository,
        IRepository<Seat> seatRepository,
        ISeatService seatService,
        IMapper mapper)
        : base(tripRepository)
        {
            _busRepository = busRepository;
            _seatRepository = seatRepository;
            _seatService = seatService;
            _mapper = mapper;
        }

        private async Task LoadBusForTripAsync(Trip trip)
        {
            if (trip.BusId != Guid.Empty)
            {
                var bus = await _busRepository.GetByIdAsync(trip.BusId);
                if (bus != null)
                {
                    trip.Bus = bus;
                }
            }
        }

        private async Task LoadBusesForTripsAsync(IEnumerable<Trip> trips)
        {
            foreach (var trip in trips)
            {
                await LoadBusForTripAsync(trip);
            }
        }

        private async Task LoadSeatsForTripAsync(Trip trip)
        {
            if (trip.Id != Guid.Empty)
            {
                var seats = await _seatRepository.FindAsync(s => s.TripId == trip.Id && s.IsActive);
                trip.Seats = seats.OrderBy(s => s.Row).ThenBy(s => s.Column).ToList();
            }
        }

        public async Task<TripResponseDto> GetTripResponseByIdAsync(Guid id)
        {
            var trip = await _repository.GetByIdAsync(id);

            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {id} não encontrada");

            await LoadBusForTripAsync(trip);
            return _mapper.Map<TripResponseDto>(trip);
        }

        public async Task<TripDetailsDto> GetTripDetailsByIdAsync(Guid id)
        {
            var trip = await _repository.GetByIdAsync(id);

            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {id} não encontrada");

            await LoadBusForTripAsync(trip);
            await LoadSeatsForTripAsync(trip);
            return _mapper.Map<TripDetailsDto>(trip);
        }

        public async Task<IEnumerable<TripResponseDto>> GetAllTripResponsesAsync()
        {
            var trips = await _repository.GetAllAsync();
            await LoadBusesForTripsAsync(trips);

            return _mapper.Map<IEnumerable<TripResponseDto>>(trips.OrderBy(t => t.DepartureTime));
        }

        public async Task<IEnumerable<TripResponseDto>> GetByBusIdAsync(Guid busId)
        {
            var busExists = await _busRepository.ExistsAsync(b => b.Id == busId);
            if (!busExists)
                throw new KeyNotFoundException($"Ônibus com ID {busId} não encontrado");

            var trips = await _repository.FindAsync(t => t.BusId == busId);
            await LoadBusesForTripsAsync(trips);

            return _mapper.Map<IEnumerable<TripResponseDto>>(trips.OrderBy(t => t.DepartureTime));
        }

        public async Task<IEnumerable<TripResponseDto>> GetByStatusAsync(TripStatus status)
        {
            var trips = await _repository.FindAsync(t => t.Status == status);
            await LoadBusesForTripsAsync(trips);

            return _mapper.Map<IEnumerable<TripResponseDto>>(trips.OrderBy(t => t.DepartureTime));
        }

        public async Task<IEnumerable<TripResponseDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var trips = await _repository.FindAsync(t =>
            t.DepartureTime >= startDate && t.DepartureTime <= endDate);

            await LoadBusesForTripsAsync(trips);

            return _mapper.Map<IEnumerable<TripResponseDto>>(trips.OrderBy(t => t.DepartureTime));
        }

        public async Task<TripResponseDto> CreateTripAsync(CreateTripDto createTripDto)
        {
            var busExists = await _busRepository.ExistsAsync(b => b.Id == createTripDto.BusId);
            if (!busExists)
                throw new KeyNotFoundException($"Ônibus com ID {createTripDto.BusId} não encontrado");

            if (createTripDto.ArrivalTime <= createTripDto.DepartureTime)
                throw new InvalidOperationException("A data de chegada deve ser posterior à data de partida");

            if (createTripDto.DepartureTime <= DateTime.Now)
                throw new InvalidOperationException("A data de partida deve ser futura");

            var trip = _mapper.Map<Trip>(createTripDto);
            var createdTrip = await _repository.AddAsync(trip);

            await LoadBusForTripAsync(createdTrip);
            return _mapper.Map<TripResponseDto>(createdTrip);
        }

        public async Task<TripDetailsDto> CreateTripWithSeatsAsync(CreateTripDto createTripDto)
        {
            // 1. Cria a viagem
            var tripResponse = await CreateTripAsync(createTripDto);

            // 2. Busca o ônibus para obter a capacidade
            var bus = await _busRepository.GetByIdAsync(createTripDto.BusId);
            if (bus == null)
                throw new KeyNotFoundException($"Ônibus com ID {createTripDto.BusId} não encontrado");

            // 3. Gera os assentos baseado na capacidade do ônibus
            var trip = await _repository.GetByIdAsync(tripResponse.Id);
            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {tripResponse.Id} não encontrada");

            await _seatService.GenerateSeatsForTripAsync(trip.Id, bus.Capacity);

            // 4. Retorna os detalhes completos
            return await GetTripDetailsByIdAsync(trip.Id);
        }

        public async Task<TripResponseDto> UpdateTripAsync(UpdateTripDto updateTripDto)
        {
            var trip = await _repository.GetByIdAsync(updateTripDto.Id);
            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {updateTripDto.Id} não encontrada");

            var busExists = await _busRepository.ExistsAsync(b => b.Id == updateTripDto.BusId);
            if (!busExists)
                throw new KeyNotFoundException($"Ônibus com ID {updateTripDto.BusId} não encontrado");

            if (updateTripDto.ArrivalTime <= updateTripDto.DepartureTime)
                throw new InvalidOperationException("A data de chegada deve ser posterior à data de partida");

            if (updateTripDto.DepartureTime <= DateTime.Now)
                throw new InvalidOperationException("A data de partida deve ser futura");

            _mapper.Map(updateTripDto, trip);
            await _repository.UpdateAsync(trip);

            await LoadBusForTripAsync(trip);
            return _mapper.Map<TripResponseDto>(trip);
        }

        public async Task DeleteTripAsync(Guid id)
        {
            var trip = await _repository.GetByIdAsync(id);
            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {id} não encontrada");

            trip.IsActive = false;
            await _repository.UpdateAsync(trip);
        }

        public async Task UpdateStatusAsync(Guid id, TripStatus newStatus)
        {
            var trip = await _repository.GetByIdAsync(id);
            if (trip == null)
                throw new KeyNotFoundException($"Viagem com ID {id} não encontrada");

            if (trip.Status == TripStatus.Completed && newStatus != TripStatus.Completed)
                throw new InvalidOperationException("Não é possível alterar o status de uma viagem já concluída");

            if (trip.Status == TripStatus.Cancelled && newStatus != TripStatus.Cancelled)
                throw new InvalidOperationException("Não é possível alterar o status de uma viagem já cancelada");

            trip.Status = newStatus;
            await _repository.UpdateAsync(trip);
        }
    }
}


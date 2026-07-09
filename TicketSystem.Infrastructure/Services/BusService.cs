using AutoMapper;
using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Infrastructure.Services
{
    public class BusService : BaseService<Bus>, IBusService
    {
        private readonly IRepository<Trip> _tripRepository;
        private readonly IMapper _mapper;

        public BusService(
            IRepository<Bus> busRepository,
            IRepository<Trip> tripRepository,
            IMapper mapper)
            : base(busRepository)
        {
            _tripRepository = tripRepository;
            _mapper = mapper;
        }

        public async Task<BusResponseDto> GetBusResponseByIdAsync(Guid id)
        {
            var bus = await _repository.GetByIdAsync(id);
            
            if (bus == null)
                throw new KeyNotFoundException($"Ônibus com ID {id} não encontrado");

            var trips = await _tripRepository.FindAsync(t => t.BusId == id);
            bus.Trips = trips.ToList();

            return _mapper.Map<BusResponseDto>(bus);
        }

        public async Task<IEnumerable<BusResponseDto>> GetAllBusResponsesAsync()
        {
            var buses = await _repository.GetAllAsync();
            
            foreach (var bus in buses)
            {
                var trips = await _tripRepository.FindAsync(t => t.BusId == bus.Id);
                bus.Trips = trips.ToList();
            }

            return _mapper.Map<IEnumerable<BusResponseDto>>(buses);
        }

        public async Task<IEnumerable<BusResponseDto>> GetActiveBusesAsync()
        {
            var buses = await _repository.FindAsync(b => b.IsActive);
            
            foreach (var bus in buses)
            {
                var trips = await _tripRepository.FindAsync(t => t.BusId == bus.Id);
                bus.Trips = trips.ToList();
            }

            return _mapper.Map<IEnumerable<BusResponseDto>>(buses);
        }

        public async Task<BusResponseDto> CreateBusAsync(CreateBusDto createBusDto)
        {
            var exists = await _repository.ExistsAsync(b => b.Plate == createBusDto.Plate);
            if (exists)
                throw new InvalidOperationException($"Já existe um ônibus com a placa {createBusDto.Plate}");

            var bus = _mapper.Map<Bus>(createBusDto);
            var createdBus = await _repository.AddAsync(bus);
            
            return _mapper.Map<BusResponseDto>(createdBus);
        }

        public async Task<BusResponseDto> UpdateBusAsync(UpdateBusDto updateBusDto)
        {
            var bus = await _repository.GetByIdAsync(updateBusDto.Id);
            if (bus == null)
                throw new KeyNotFoundException($"Ônibus com ID {updateBusDto.Id} não encontrado");

            var exists = await _repository.ExistsAsync(b => b.Plate == updateBusDto.Plate && b.Id != updateBusDto.Id);
            if (exists)
                throw new InvalidOperationException($"Já existe um ônibus com a placa {updateBusDto.Plate}");

            _mapper.Map(updateBusDto, bus);
            await _repository.UpdateAsync(bus);
            
            return _mapper.Map<BusResponseDto>(bus);
        }

        public async Task DeleteBusAsync(Guid id)
        {
            var bus = await _repository.GetByIdAsync(id);
            if (bus == null)
                throw new KeyNotFoundException($"Ônibus com ID {id} não encontrado");

            var activeTrips = await _tripRepository.FindAsync(t => t.BusId == id && t.IsActive && t.Status != TripStatus.Completed);
            if (activeTrips.Any())
                throw new InvalidOperationException("Não é possível excluir um ônibus com viagens ativas");

            bus.IsActive = false;
            bus.UpdatedAt = DateTime.Now;
            await _repository.UpdateAsync(bus);
        }

        public async Task<bool> ExistsByPlateAsync(string plate)
        {
            return await _repository.ExistsAsync(b => b.Plate == plate);
        }

        public async Task<bool> ExistsByPlateAsync(string plate, Guid excludeId)
        {
            return await _repository.ExistsAsync(b => b.Plate == plate && b.Id != excludeId);
        }
    }
}
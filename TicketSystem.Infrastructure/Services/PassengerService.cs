using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.DTOs.Passenger;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Infrastructure.Services
{
    public class PassengerService : BaseService<Passenger>, IPassengerService
    {
        private readonly IRepository<Reservation> _reservationRepository;
        private readonly IMapper _mapper;

        public PassengerService(
        IRepository<Passenger> passengerRepository,
        IRepository<Reservation> reservationRepository,
        IMapper mapper)
        : base(passengerRepository)
        {
            _reservationRepository = reservationRepository;
            _mapper = mapper;
        }

        public async Task<Passenger> GetOrCreatePassengerAsync(PassengerInfoDto passengerInfo)
        {
            var passengers = await _repository.FindAsync(p => p.Document == passengerInfo.Document);
            var existing = passengers.FirstOrDefault();

            if (existing != null)
            {
                existing.Name = passengerInfo.Name;
                existing.Email = passengerInfo.Email;
                existing.Phone = passengerInfo.Phone;
                await _repository.UpdateAsync(existing);
                return existing;
            }

            var newPassenger = new Passenger
            {
                Name = passengerInfo.Name,
                Document = passengerInfo.Document,
                Email = passengerInfo.Email,
                Phone = passengerInfo.Phone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            return await _repository.AddAsync(newPassenger);
        }

        public async Task<Passenger> GetPassengerByDocumentAsync(string document)
        {
            var passengers = await _repository.FindAsync(p => p.Document == document);
            return passengers.FirstOrDefault() ?? throw new KeyNotFoundException($"Passageiro com CPF {document} não encontrado");
        }

        public async Task<bool> HasPendingReservationForTripAsync(string document, Guid tripId)
        {
            var passengers = await _repository.FindAsync(p => p.Document == document);
            var passenger = passengers.FirstOrDefault();

            if (passenger == null)
                return false;

            var reservations = await _reservationRepository.FindAsync(r =>
            r.PassengerId == passenger.Id &&
            r.TripId == tripId &&
            r.Status == ReservationStatus.Pending &&
            r.ExpiresAt > DateTime.UtcNow);

            return reservations.Any();
        }

        public async Task<PassengerDto> CreatePassengerAsync(CreatePassengerDto createDto)
        {
            var exists = await _repository.ExistsAsync(p => p.Document == createDto.Document);
            if (exists)
                throw new InvalidOperationException($"Já existe um passageiro com o CPF {createDto.Document}");

            var passenger = new Passenger
            {
                Name = createDto.Name,
                Document = createDto.Document,
                Email = createDto.Email,
                Phone = createDto.Phone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.AddAsync(passenger);
            return MapToDto(created);
        }

        public async Task<PassengerDto> UpdatePassengerAsync(UpdatePassengerDto updateDto)
        {
            var passenger = await _repository.GetByIdAsync(updateDto.Id);
            if (passenger == null)
                throw new KeyNotFoundException($"Passageiro com ID {updateDto.Id} não encontrado");

            var exists = await _repository.ExistsAsync(p => p.Document == updateDto.Document && p.Id != updateDto.Id);
            if (exists)
                throw new InvalidOperationException($"Já existe um passageiro com o CPF {updateDto.Document}");

            passenger.Name = updateDto.Name;
            passenger.Document = updateDto.Document;
            passenger.Email = updateDto.Email;
            passenger.Phone = updateDto.Phone;
            passenger.IsActive = updateDto.IsActive;
            passenger.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(passenger);
            return MapToDto(passenger);
        }

        public async Task DeletePassengerAsync(Guid id)
        {
            var passenger = await _repository.GetByIdAsync(id);
            if (passenger == null)
                throw new KeyNotFoundException($"Passageiro com ID {id} não encontrado");

            var hasReservations = await _reservationRepository.ExistsAsync(r => r.PassengerId == id && r.IsActive);
            if (hasReservations)
                throw new InvalidOperationException("Não é possível excluir um passageiro com reservas ativas");

            passenger.IsActive = false;
            passenger.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(passenger);
        }

        public PassengerDto MapToDto(Passenger passenger)
        {
            var dto = _mapper.Map<PassengerDto>(passenger);

            // Contar reservas ativas do passageiro
            var reservationCount = _reservationRepository.CountAsync(r => r.PassengerId == passenger.Id && r.IsActive).GetAwaiter().GetResult();
            dto.TotalReservations = reservationCount;

            return dto;
        }

        public IEnumerable<PassengerDto> MapToDto(IEnumerable<Passenger> passengers)
        {
            return passengers.Select(MapToDto);
        }
    }
}

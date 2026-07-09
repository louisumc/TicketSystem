using TicketSystem.Application.DTOs.Passenger;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Interfaces
{
    public interface IPassengerService : IService<Passenger>
    {
        Task<Passenger> GetOrCreatePassengerAsync(PassengerInfoDto passengerInfo);
        Task<Passenger> GetPassengerByDocumentAsync(string document);
        Task<bool> HasPendingReservationForTripAsync(string document, Guid tripId);
        Task<PassengerDto> CreatePassengerAsync(CreatePassengerDto createDto);
        Task<PassengerDto> UpdatePassengerAsync(UpdatePassengerDto updateDto);
        Task DeletePassengerAsync(Guid id);
        PassengerDto MapToDto(Passenger passenger);
        IEnumerable<PassengerDto> MapToDto(IEnumerable<Passenger> passengers);
    }
}


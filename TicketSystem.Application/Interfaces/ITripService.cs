using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Interfaces
{
    public interface ITripService : IService<Trip>
    {
        // Métodos específicos de Trip
        Task<TripResponseDto> GetTripResponseByIdAsync(Guid id);
        Task<TripDetailsDto> GetTripDetailsByIdAsync(Guid id);
        Task<IEnumerable<TripResponseDto>> GetAllTripResponsesAsync();
        Task<IEnumerable<TripResponseDto>> GetByBusIdAsync(Guid busId);
        Task<IEnumerable<TripResponseDto>> GetByStatusAsync(TripStatus status);
        Task<IEnumerable<TripResponseDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<TripResponseDto> CreateTripAsync(CreateTripDto createTripDto);
        Task<TripDetailsDto> CreateTripWithSeatsAsync(CreateTripDto createTripDto);
        Task<TripResponseDto> UpdateTripAsync(UpdateTripDto updateTripDto);
        Task DeleteTripAsync(Guid id);
        Task UpdateStatusAsync(Guid id, TripStatus newStatus);
    }
}

using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.Interfaces
{
    public interface ISeatService : IService<Seat>
    {
        Task<SeatDto> GetSeatByIdAsync(Guid id);
        Task<IEnumerable<SeatDto>> GetSeatsByTripIdAsync(Guid tripId);
        Task<SeatDto> CreateSeatAsync(CreateSeatDto createSeatDto);
        Task<SeatDto> UpdateSeatAsync(UpdateSeatDto updateSeatDto);
        Task DeleteSeatAsync(Guid id);
        Task<SeatDto> UpdateSeatStatusAsync(Guid seatId, UpdateSeatStatusDto updateDto);
        Task<IEnumerable<SeatDto>> GenerateSeatsForTripAsync(Guid tripId, int capacity);
        Task<bool> IsSeatNumberAvailableAsync(Guid tripId, string seatNumber);
        Task<bool> IsSeatNumberAvailableAsync(Guid tripId, string seatNumber, Guid excludeId);
    }
}
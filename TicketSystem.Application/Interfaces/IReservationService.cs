using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Interfaces
{
    public interface IReservationService : IService<Reservation>
    {
        Task<ReservationDto> CreateReservationAsync(CreateReservationDto createDto);
        Task<ReservationDto> GetReservationByIdAsync(Guid id);
        Task<ReservationDto> ConfirmReservationAsync(ConfirmReservationDto confirmDto);
        Task CancelReservationAsync(Guid reservationId);
        Task<AvailableSeatsDto> GetAvailableSeatsAsync(Guid tripId);
        Task<IEnumerable<ReservationDto>> GetReservationsByTripIdAsync(Guid tripId);
        Task<IEnumerable<ReservationDto>> GetReservationsByPassengerDocumentAsync(string document);
        Task ExpirePendingReservationsAsync();
        Task<IEnumerable<ExpiredReservationDto>> GetExpiredReservationsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<ReservationDto>> GetAllReservationsAsync();
    }
}
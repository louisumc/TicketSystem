using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.DTOs.Reservation
{
    public class ReservationDto
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public string TripOrigin { get; set; } = string.Empty;
        public string TripDestination { get; set; } = string.Empty;
        public DateTime TripDepartureTime { get; set; }
        public Guid PassengerId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerDocument { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public string PassengerPhone { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ReservationStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public List<ReservationSeatDto> Seats { get; set; } = new List<ReservationSeatDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class ReservationSeatDto
    {
        public Guid Id { get; set; }
        public Guid SeatId { get; set; }
        public string SeatNumber { get; set; } = string.Empty;
        public SeatType SeatType { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public decimal Price { get; set; }
    }
}
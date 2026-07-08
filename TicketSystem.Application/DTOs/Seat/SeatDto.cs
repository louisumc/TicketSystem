using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.DTOs.Seat
{
    public class SeatDto
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public string Number { get; set; } = string.Empty;
        public SeatType Type { get; set; }
        public SeatStatus Status { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public string? PassengerName { get; set; }
        public string? PassengerDocument { get; set; }
        public decimal? PriceMultiplier { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
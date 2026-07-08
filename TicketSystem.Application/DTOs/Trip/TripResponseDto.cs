using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.DTOs.Trip
{
    public class TripResponseDto
    {
        public Guid Id { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public Guid BusId { get; set; }
        public string BusPlate { get; set; } = string.Empty;
        public string BusModel { get; set; } = string.Empty;
        public string BusCompany { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public TripStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}

namespace TicketSystem.Application.DTOs.Bus
{
    public class BusResponseDto
    {
        public Guid Id { get; set; }
        public string Plate { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public int TotalTrips { get; set; }
    }
}
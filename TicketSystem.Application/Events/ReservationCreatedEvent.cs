namespace TicketSystem.Application.Events
{
    public class ReservationCreatedEvent
    {
        public Guid ReservationId { get; set; }
        public Guid TripId { get; set; }
        public Guid PassengerId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public string PassengerDocument { get; set; } = string.Empty;
        public List<string> SeatNumbers { get; set; } = new List<string>();
        public decimal TotalAmount { get; set; }
        public DateTime ReservationDate { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string TripOrigin { get; set; } = string.Empty;
        public string TripDestination { get; set; } = string.Empty;
        public DateTime TripDepartureTime { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
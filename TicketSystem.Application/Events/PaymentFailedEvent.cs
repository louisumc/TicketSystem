namespace TicketSystem.Application.Events
{
    public class PaymentFailedEvent
    {
        public Guid ReservationId { get; set; }
        public Guid TripId { get; set; }
        public Guid PassengerId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public List<string> SeatNumbers { get; set; } = new List<string>();
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public DateTime FailedAt { get; set; }
    }
}
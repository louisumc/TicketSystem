namespace TicketSystem.Application.Events
{
    public class ReservationExpiredEvent
    {
        public Guid ReservationId { get; set; }
        public Guid TripId { get; set; }
        public Guid PassengerId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public List<string> SeatNumbers { get; set; } = new List<string>();
        public DateTime ExpiredAt { get; set; }
    }
}
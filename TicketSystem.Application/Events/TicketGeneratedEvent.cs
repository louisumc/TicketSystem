namespace TicketSystem.Application.Events
{
    public class TicketGeneratedEvent
    {
        public Guid ReservationId { get; set; }
        public string TicketCode { get; set; } = string.Empty;
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public string PassengerDocument { get; set; } = string.Empty;
        public List<TicketSeatInfo> Seats { get; set; } = new List<TicketSeatInfo>();
        public string TripOrigin { get; set; } = string.Empty;
        public string TripDestination { get; set; } = string.Empty;
        public DateTime TripDepartureTime { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string QrCode { get; set; } = string.Empty;
    }

    public class TicketSeatInfo
    {
        public string Number { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
    }
}
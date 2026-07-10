namespace TicketSystem.Application.DTOs.Reservation
{
    public class ExpiredReservationDto
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public Guid PassengerId { get; set; }
        public string PassengerName { get; set; } = string.Empty;
        public string PassengerEmail { get; set; } = string.Empty;
        public List<ExpiredSeatInfo> Seats { get; set; } = new List<ExpiredSeatInfo>();
        public DateTime ExpiresAt { get; set; }
    }

    public class ExpiredSeatInfo
    {
        public string SeatNumber { get; set; } = string.Empty;
        public Guid SeatId { get; set; }
    }
}
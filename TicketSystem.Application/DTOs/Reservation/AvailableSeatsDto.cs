using TicketSystem.Application.DTOs.Seat;

namespace TicketSystem.Application.DTOs.Reservation
{
    public class AvailableSeatsDto
    {
        public Guid TripId { get; set; }
        public string TripOrigin { get; set; } = string.Empty;
        public string TripDestination { get; set; } = string.Empty;
        public DateTime TripDepartureTime { get; set; }
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int SoldSeats { get; set; }
        public List<SeatDto> Seats { get; set; } = new List<SeatDto>();
    }
}
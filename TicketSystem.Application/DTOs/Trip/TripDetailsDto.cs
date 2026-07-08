using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.DTOs.Trip
{
    public class TripDetailsDto : TripResponseDto
    {
        public List<SeatDto> Seats { get; set; } = new List<SeatDto>();
        public int TotalSeats { get; set; }
        public int AvailableSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int SoldSeats { get; set; }
        public int MaintenanceSeats { get; set; }
    }
}


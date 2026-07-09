using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities
{
    public class Seat : BaseEntity
    {
        [Required]
        public Guid TripId { get; set; }

        [Required]
        [MaxLength(10)]
        public string Number { get; set; } = string.Empty;

        [Required]
        public SeatType Type { get; set; }

        [Required]
        public SeatStatus Status { get; set; }

        [Required]
        [Range(1, 99)]
        public int Row { get; set; }

        [Required]
        [Range(1, 10)]
        public int Column { get; set; }

        [MaxLength(50)]
        public string? PassengerName { get; set; }

        [MaxLength(20)]
        public string? PassengerDocument { get; set; }

        public decimal? PriceMultiplier { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public virtual Trip Trip { get; set; } = null!;
        public virtual ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();
    }
}
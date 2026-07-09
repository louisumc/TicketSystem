using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities
{
    public class ReservationSeat : BaseEntity
    {
        [Required]
        public Guid ReservationId { get; set; }

        [Required]
        public Guid SeatId { get; set; }

        [Required]
        [Range(0, 99999.99)]
        public decimal Price { get; set; }

        public virtual Reservation Reservation { get; set; } = null!;
        public virtual Seat Seat { get; set; } = null!;
    }
}
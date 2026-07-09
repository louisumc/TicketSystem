using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities
{
    public class Reservation : BaseEntity
    {
        [Required]
        public Guid TripId { get; set; }

        [Required]
        public Guid PassengerId { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        [Required]
        public ReservationStatus Status { get; set; }

        [Required]
        [Range(0, 99999.99)]
        public decimal TotalAmount { get; set; }

        public virtual Trip Trip { get; set; } = null!;
        public virtual Passenger Passenger { get; set; } = null!;
        public virtual ICollection<ReservationSeat> ReservationSeats { get; set; } = new List<ReservationSeat>();
    }
}
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities
{
    public class Passenger : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Document { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
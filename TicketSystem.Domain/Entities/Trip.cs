using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Domain.Entities
{
    public class Trip : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Origin { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public DateTime DepartureTime { get; set; }

        [Required]
        public DateTime ArrivalTime { get; set; }

        [Required]
        public Guid BusId { get; set; }

        [Required]
        [Range(0, 99999.99)]
        public decimal Price { get; set; }

        [Required]
        public TripStatus Status { get; set; }

        // Navigation property
        public virtual Bus Bus { get; set; } = null!;
    }
}
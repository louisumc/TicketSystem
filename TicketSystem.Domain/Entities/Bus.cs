using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Domain.Entities
{
    public class Bus : BaseEntity
    {
        [Required]
        [MaxLength(10)]
        public string Plate { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Company { get; set; } = string.Empty;

        [Required]
        [Range(1, 100)]
        public int Capacity { get; set; }

        // Navigation property
        public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    }
}
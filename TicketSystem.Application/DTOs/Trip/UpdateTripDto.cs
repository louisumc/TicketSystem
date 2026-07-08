using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.DTOs.Trip
{
    public class UpdateTripDto
    {
        [Required(ErrorMessage = "O ID é obrigatório")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "A origem é obrigatória")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "A origem deve ter entre 2 e 100 caracteres")]
        public string Origin { get; set; } = string.Empty;

        [Required(ErrorMessage = "O destino é obrigatório")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "O destino deve ter entre 2 e 100 caracteres")]
        public string Destination { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data de partida é obrigatória")]
        public DateTime DepartureTime { get; set; }

        [Required(ErrorMessage = "A data de chegada é obrigatória")]
        public DateTime ArrivalTime { get; set; }

        [Required(ErrorMessage = "O ID do ônibus é obrigatório")]
        public Guid BusId { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório")]
        [Range(0.01, 99999.99, ErrorMessage = "O preço deve ser entre 0.01 e 99999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "O status é obrigatório")]
        public TripStatus Status { get; set; }

        public bool IsActive { get; set; }
    }
}
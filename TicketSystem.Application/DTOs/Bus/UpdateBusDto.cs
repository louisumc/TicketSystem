using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Application.DTOs.Bus
{
    public class UpdateBusDto
    {
        [Required(ErrorMessage = "O ID é obrigatório")]
        public Guid Id { get; set; }

        [Required(ErrorMessage = "A placa é obrigatória")]
        [StringLength(10, MinimumLength = 7, ErrorMessage = "A placa deve ter 7 caracteres")]
        public string Plate { get; set; } = string.Empty;

        [Required(ErrorMessage = "O modelo é obrigatório")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "O modelo deve ter entre 2 e 50 caracteres")]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "A empresa é obrigatória")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "A empresa deve ter entre 2 e 100 caracteres")]
        public string Company { get; set; } = string.Empty;

        [Required(ErrorMessage = "A capacidade é obrigatória")]
        [Range(1, 100, ErrorMessage = "A capacidade deve ser entre 1 e 100")]
        public int Capacity { get; set; }

        public bool IsActive { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Application.DTOs.Passenger
{
    public class CreatePassengerDto
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O CPF é obrigatório")]
        [StringLength(20, MinimumLength = 11, ErrorMessage = "O CPF deve ter entre 11 e 20 caracteres")]
        [RegularExpression(@"^[0-9]{11}$", ErrorMessage = "CPF deve conter apenas números")]
        public string Document { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(100, ErrorMessage = "O email deve ter no máximo 100 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório")]
        [StringLength(20, MinimumLength = 10, ErrorMessage = "O telefone deve ter entre 10 e 20 caracteres")]
        public string Phone { get; set; } = string.Empty;
    }
}
using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.DTOs.Seat
{
    public class CreateSeatDto
    {
        [Required(ErrorMessage = "O ID da viagem é obrigatório")]
        public Guid TripId { get; set; }

        [Required(ErrorMessage = "O número do assento é obrigatório")]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "O número deve ter entre 2 e 10 caracteres")]
        public string Number { get; set; } = string.Empty;

        [Required(ErrorMessage = "O tipo de assento é obrigatório")]
        public SeatType Type { get; set; }

        [Required(ErrorMessage = "O status do assento é obrigatório")]
        public SeatStatus Status { get; set; }

        [Required(ErrorMessage = "A fileira é obrigatória")]
        [Range(1, 99, ErrorMessage = "A fileira deve ser entre 1 e 99")]
        public int Row { get; set; }

        [Required(ErrorMessage = "A coluna é obrigatória")]
        [Range(1, 10, ErrorMessage = "A coluna deve ser entre 1 e 10")]
        public int Column { get; set; }

        [StringLength(50, ErrorMessage = "O nome do passageiro deve ter no máximo 50 caracteres")]
        public string? PassengerName { get; set; }

        [StringLength(20, ErrorMessage = "O documento do passageiro deve ter no máximo 20 caracteres")]
        public string? PassengerDocument { get; set; }

        [Range(0.5, 2.0, ErrorMessage = "O multiplicador deve ser entre 0.5 e 2.0")]
        public decimal? PriceMultiplier { get; set; }
    }
}
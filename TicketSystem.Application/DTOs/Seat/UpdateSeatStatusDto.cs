using System.ComponentModel.DataAnnotations;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Application.DTOs.Seat
{
    public class UpdateSeatStatusDto
    {
        [Required(ErrorMessage = "O status é obrigatório")]
        public SeatStatus Status { get; set; }

        [StringLength(50, ErrorMessage = "O nome do passageiro deve ter no máximo 50 caracteres")]
        public string? PassengerName { get; set; }

        [StringLength(20, ErrorMessage = "O documento do passageiro deve ter no máximo 20 caracteres")]
        public string? PassengerDocument { get; set; }
    }
}
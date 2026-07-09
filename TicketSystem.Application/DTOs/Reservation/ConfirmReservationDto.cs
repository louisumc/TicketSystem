using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Application.DTOs.Reservation
{
    public class ConfirmReservationDto
    {
        [Required(ErrorMessage = "O ID da reserva é obrigatório")]
        public Guid ReservationId { get; set; }

        [Required(ErrorMessage = "O método de pagamento é obrigatório")]
        [StringLength(50, ErrorMessage = "O método de pagamento deve ter no máximo 50 caracteres")]
        public string PaymentMethod { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres")]
        public string? Observations { get; set; }
    }
}
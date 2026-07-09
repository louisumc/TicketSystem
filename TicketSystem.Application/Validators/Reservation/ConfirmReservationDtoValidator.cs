using FluentValidation;
using TicketSystem.Application.DTOs.Reservation;

namespace TicketSystem.Application.Validators.Reservation
{
    public class ConfirmReservationDtoValidator : AbstractValidator<ConfirmReservationDto>
    {
        public ConfirmReservationDtoValidator()
        {
            RuleFor(x => x.ReservationId)
            .NotEmpty().WithMessage("O ID da reserva é obrigatório");

            RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("O método de pagamento é obrigatório")
            .MaximumLength(50).WithMessage("O método de pagamento deve ter no máximo 50 caracteres");

            RuleFor(x => x.Observations)
            .MaximumLength(500).WithMessage("As observações devem ter no máximo 500 caracteres");
        }
    }
}
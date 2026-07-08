using FluentValidation;
using TicketSystem.Application.DTOs.Seat;

namespace TicketSystem.Application.Validators.Seat
{
    public class UpdateSeatStatusDtoValidator : AbstractValidator<UpdateSeatStatusDto>
    {
        public UpdateSeatStatusDtoValidator()
        {
            RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido");

            RuleFor(x => x.PassengerName)
            .MaximumLength(50).WithMessage("O nome do passageiro deve ter no máximo 50 caracteres");

            RuleFor(x => x.PassengerDocument)
            .MaximumLength(20).WithMessage("O documento do passageiro deve ter no máximo 20 caracteres");
        }
    }
}


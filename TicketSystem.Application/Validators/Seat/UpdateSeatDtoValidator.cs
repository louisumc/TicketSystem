using FluentValidation;
using TicketSystem.Application.DTOs.Seat;

namespace TicketSystem.Application.Validators.Seat
{
    public class UpdateSeatDtoValidator : AbstractValidator<UpdateSeatDto>
    {
        public UpdateSeatDtoValidator()
        {
            RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O ID é obrigatório");

            RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("O ID da viagem é obrigatório");

            RuleFor(x => x.Number)
            .NotEmpty().WithMessage("O número do assento é obrigatório")
            .Length(2, 10).WithMessage("O número deve ter entre 2 e 10 caracteres")
            .Matches(@"^[0-9]+[A-Z]$").WithMessage("Formato inválido. Use número + letra (ex: 1A, 10B)");

            RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de assento inválido");

            RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status de assento inválido");

            RuleFor(x => x.Row)
            .NotEmpty().WithMessage("A fileira é obrigatória")
            .InclusiveBetween(1, 99).WithMessage("A fileira deve ser entre 1 e 99");

            RuleFor(x => x.Column)
            .NotEmpty().WithMessage("A coluna é obrigatória")
            .InclusiveBetween(1, 10).WithMessage("A coluna deve ser entre 1 e 10");

            RuleFor(x => x.PriceMultiplier)
            .InclusiveBetween(0.5m, 2.0m).When(x => x.PriceMultiplier.HasValue)
            .WithMessage("O multiplicador deve ser entre 0.5 e 2.0");
        }
    }
}
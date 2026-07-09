using FluentValidation;
using TicketSystem.Application.DTOs.Trip;

namespace TicketSystem.Application.Validators.Trip
{
    public class UpdateTripDtoValidator : AbstractValidator<UpdateTripDto>
    {
        public UpdateTripDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("O ID é obrigatório");

            RuleFor(x => x.Origin)
                .NotEmpty().WithMessage("A origem é obrigatória")
                .Length(2, 100).WithMessage("A origem deve ter entre 2 e 100 caracteres");

            RuleFor(x => x.Destination)
                .NotEmpty().WithMessage("O destino é obrigatório")
                .Length(2, 100).WithMessage("O destino deve ter entre 2 e 100 caracteres");

            RuleFor(x => x.DepartureTime)
                .NotEmpty().WithMessage("A data de partida é obrigatória")
                .GreaterThan(DateTime.Now).WithMessage("A data de partida deve ser futura");

            RuleFor(x => x.ArrivalTime)
                .NotEmpty().WithMessage("A data de chegada é obrigatória")
                .GreaterThan(x => x.DepartureTime).WithMessage("A data de chegada deve ser posterior à data de partida");

            RuleFor(x => x.BusId)
                .NotEmpty().WithMessage("O ID do ônibus é obrigatório");

            RuleFor(x => x.Price)
                .NotEmpty().WithMessage("O preço é obrigatório")
                .InclusiveBetween(0.01m, 99999.99m).WithMessage("O preço deve ser entre 0.01 e 99999.99");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Status inválido");
        }
    }
}

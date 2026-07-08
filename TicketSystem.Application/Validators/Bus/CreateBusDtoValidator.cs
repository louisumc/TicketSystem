using FluentValidation;
using TicketSystem.Application.DTOs.Bus;

namespace TicketSystem.Application.Validators.Bus
{
    public class CreateBusDtoValidator : AbstractValidator<CreateBusDto>
    {
        public CreateBusDtoValidator()
        {
            RuleFor(x => x.Plate)
                .NotEmpty().WithMessage("A placa é obrigatória")
                .Length(7).WithMessage("A placa deve ter 7 caracteres")
                .Matches(@"^[A-Z]{3}[0-9]{4}$|^[A-Z]{3}[0-9][A-Z][0-9]{2}$")
                .WithMessage("Formato de placa inválido. Use formato antigo (ABC1234) ou Mercosul (ABC1D23)");

            RuleFor(x => x.Model)
                .NotEmpty().WithMessage("O modelo é obrigatório")
                .Length(2, 50).WithMessage("O modelo deve ter entre 2 e 50 caracteres");

            RuleFor(x => x.Company)
                .NotEmpty().WithMessage("A empresa é obrigatória")
                .Length(2, 100).WithMessage("A empresa deve ter entre 2 e 100 caracteres");

            RuleFor(x => x.Capacity)
                .NotEmpty().WithMessage("A capacidade é obrigatória")
                .InclusiveBetween(1, 100).WithMessage("A capacidade deve ser entre 1 e 100");
        }
    }
}
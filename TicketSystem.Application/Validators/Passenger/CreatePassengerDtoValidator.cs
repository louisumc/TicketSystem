using FluentValidation;
using TicketSystem.Application.DTOs.Passenger;

namespace TicketSystem.Application.Validators.Passenger
{
    public class CreatePassengerDtoValidator : AbstractValidator<CreatePassengerDto>
    {
        public CreatePassengerDtoValidator()
        {
            RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome é obrigatório")
            .Length(3, 100).WithMessage("O nome deve ter entre 3 e 100 caracteres");

            RuleFor(x => x.Document)
            .NotEmpty().WithMessage("O CPF é obrigatório")
            .Length(11, 20).WithMessage("O CPF deve ter entre 11 e 20 caracteres")
            .Matches(@"^[0-9]{11}$").WithMessage("CPF deve conter apenas números");

            RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(100).WithMessage("O email deve ter no máximo 100 caracteres");

            RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("O telefone é obrigatório")
            .Length(10, 20).WithMessage("O telefone deve ter entre 10 e 20 caracteres");
        }
    }
}
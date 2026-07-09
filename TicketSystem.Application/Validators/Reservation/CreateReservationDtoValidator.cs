using FluentValidation;
using TicketSystem.Application.DTOs.Reservation;

namespace TicketSystem.Application.Validators.Reservation
{
    public class CreateReservationDtoValidator : AbstractValidator<CreateReservationDto>
    {
        public CreateReservationDtoValidator()
        {
            RuleFor(x => x.TripId)
            .NotEmpty().WithMessage("O ID da viagem é obrigatório");

            RuleFor(x => x.Passenger)
            .NotNull().WithMessage("Os dados do passageiro são obrigatórios");

            RuleFor(x => x.Passenger.Name)
            .NotEmpty().WithMessage("O nome é obrigatório")
            .Length(3, 100).WithMessage("O nome deve ter entre 3 e 100 caracteres");

            RuleFor(x => x.Passenger.Document)
            .NotEmpty().WithMessage("O CPF é obrigatório")
            .Length(11, 20).WithMessage("O CPF deve ter entre 11 e 20 caracteres")
            .Matches(@"^[0-9]{11}$").WithMessage("CPF deve conter apenas números");

            RuleFor(x => x.Passenger.Email)
            .NotEmpty().WithMessage("O email é obrigatório")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(100).WithMessage("O email deve ter no máximo 100 caracteres");

            RuleFor(x => x.Passenger.Phone)
            .NotEmpty().WithMessage("O telefone é obrigatório")
            .Length(10, 20).WithMessage("O telefone deve ter entre 10 e 20 caracteres");

            RuleFor(x => x.SeatNumbers)
            .NotEmpty().WithMessage("Pelo menos um assento deve ser selecionado")
            .Must(x => x.Distinct().Count() == x.Count)
            .WithMessage("Não é permitido assentos duplicados");
        }
    }
}
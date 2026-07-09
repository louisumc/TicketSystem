using FluentAssertions;
using FluentValidation.TestHelper;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Validators.Reservation;
using Xunit;

namespace TicketSystem.Tests.Application.Validators
{
    public class CreateReservationDtoValidatorTests
    {
        private readonly CreateReservationDtoValidator _validator;

        public CreateReservationDtoValidatorTests()
        {
            _validator = new CreateReservationDtoValidator();
        }

        [Fact]
        public void Validate_ShouldPass_WhenAllFieldsAreValid()
        {
            var dto = new CreateReservationDto
            {
                TripId = Guid.NewGuid(),
                Passenger = new PassengerInfoDto
                {
                    Name = "João Silva",
                    Document = "12345678901",
                    Email = "joao@email.com",
                    Phone = "11999999999"
                },
                SeatNumbers = new List<string> { "1A" }
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldFail_WhenTripIdIsEmpty()
        {
            var dto = new CreateReservationDto
            {
                TripId = Guid.Empty,
                Passenger = new PassengerInfoDto
                {
                    Name = "João Silva",
                    Document = "12345678901",
                    Email = "joao@email.com",
                    Phone = "11999999999"
                },
                SeatNumbers = new List<string> { "1A" }
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.TripId)
            .WithErrorMessage("O ID da viagem é obrigatório");
        }

        [Fact]
        public void Validate_ShouldFail_WhenPassengerNameIsEmpty()
        {
            var dto = new CreateReservationDto
            {
                TripId = Guid.NewGuid(),
                Passenger = new PassengerInfoDto
                {
                    Name = "",
                    Document = "12345678901",
                    Email = "joao@email.com",
                    Phone = "11999999999"
                },
                SeatNumbers = new List<string> { "1A" }
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Passenger.Name)
            .WithErrorMessage("O nome é obrigatório");
        }

        [Fact]
        public void Validate_ShouldFail_WhenDocumentIsInvalid()
        {
            var dto = new CreateReservationDto
            {
                TripId = Guid.NewGuid(),
                Passenger = new PassengerInfoDto
                {
                    Name = "João Silva",
                    Document = "123",
                    Email = "joao@email.com",
                    Phone = "11999999999"
                },
                SeatNumbers = new List<string> { "1A" }
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Passenger.Document);
        }

        [Fact]
        public void Validate_ShouldFail_WhenSeatNumbersIsEmpty()
        {
            var dto = new CreateReservationDto
            {
                TripId = Guid.NewGuid(),
                Passenger = new PassengerInfoDto
                {
                    Name = "João Silva",
                    Document = "12345678901",
                    Email = "joao@email.com",
                    Phone = "11999999999"
                },
                SeatNumbers = new List<string>()
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.SeatNumbers)
            .WithErrorMessage("Pelo menos um assento deve ser selecionado");
        }

        [Fact]
        public void Validate_ShouldFail_WhenSeatNumbersHaveDuplicates()
        {
            var dto = new CreateReservationDto
            {
                TripId = Guid.NewGuid(),
                Passenger = new PassengerInfoDto
                {
                    Name = "João Silva",
                    Document = "12345678901",
                    Email = "joao@email.com",
                    Phone = "11999999999"
                },
                SeatNumbers = new List<string> { "1A", "1A" }
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.SeatNumbers)
            .WithErrorMessage("Não é permitido assentos duplicados");
        }
    }
}
using FluentAssertions;
using FluentValidation.TestHelper;
using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Application.Validators.Seat;
using TicketSystem.Domain.Enums;
using Xunit;

namespace TicketSystem.Tests.Application.Validators
{
    public class CreateSeatDtoValidatorTests
    {
        private readonly CreateSeatDtoValidator _validator;

        public CreateSeatDtoValidatorTests()
        {
            _validator = new CreateSeatDtoValidator();
        }

        [Fact]
        public void Validate_ShouldPass_WhenAllFieldsAreValid()
        {
            var dto = new CreateSeatDto
            {
                TripId = Guid.NewGuid(),
                Number = "1A",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 1,
                Column = 1,
                PriceMultiplier = 1.10m
            };

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldFail_WhenNumberHasInvalidFormat()
        {
            var dto = new CreateSeatDto
            {
                TripId = Guid.NewGuid(),
                Number = "A1",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 1,
                Column = 1
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Number)
            .WithErrorMessage("Formato inválido. Use número + letra (ex: 1A, 10B)");
        }

        [Fact]
        public void Validate_ShouldFail_WhenRowIsOutOfRange()
        {
            var dto = new CreateSeatDto
            {
                TripId = Guid.NewGuid(),
                Number = "1A",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 0,
                Column = 1
            };

            var result = _validator.TestValidate(dto);

            result.ShouldHaveValidationErrorFor(x => x.Row)
            .WithErrorMessage("A fileira deve ser entre 1 e 99");
        }
    }
}


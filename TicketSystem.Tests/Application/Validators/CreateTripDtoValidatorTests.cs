using FluentAssertions;
using FluentValidation.TestHelper;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Application.Validators.Trip;
using TicketSystem.Domain.Enums;
using Xunit;

namespace TicketSystem.Tests.Application.Validators
{
    public class CreateTripDtoValidatorTests
    {
        private readonly CreateTripDtoValidator _validator;

        public CreateTripDtoValidatorTests()
        {
            _validator = new CreateTripDtoValidator();
        }

        [Fact]
        public void Validate_ShouldPass_WhenAllFieldsAreValid()
        {
            
            var dto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Rio de Janeiro",
                DepartureTime = DateTime.Now.AddHours(8),
                ArrivalTime = DateTime.Now.AddHours(11),
                BusId = Guid.NewGuid(),
                Price = 120.00m,
                Status = TripStatus.Scheduled
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldFail_WhenOriginIsEmpty()
        {
            
            var dto = new CreateTripDto
            {
                Origin = "",
                Destination = "Rio de Janeiro",
                DepartureTime = DateTime.Now.AddHours(8),
                ArrivalTime = DateTime.Now.AddHours(11),
                BusId = Guid.NewGuid(),
                Price = 120.00m
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.Origin)
                .WithErrorMessage("A origem é obrigatória");
        }

        [Fact]
        public void Validate_ShouldFail_WhenDepartureTimeIsPast()
        {
            
            var dto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Rio de Janeiro",
                DepartureTime = DateTime.Now.AddHours(-1),
                ArrivalTime = DateTime.Now.AddHours(2),
                BusId = Guid.NewGuid(),
                Price = 120.00m
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.DepartureTime)
                .WithErrorMessage("A data de partida deve ser futura");
        }

        [Fact]
        public void Validate_ShouldFail_WhenArrivalTimeIsBeforeDeparture()
        {
            
            var dto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Rio de Janeiro",
                DepartureTime = DateTime.Now.AddHours(8),
                ArrivalTime = DateTime.Now.AddHours(5),
                BusId = Guid.NewGuid(),
                Price = 120.00m
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.ArrivalTime)
                .WithErrorMessage("A data de chegada deve ser posterior à data de partida");
        }

        [Fact]
        public void Validate_ShouldFail_WhenPriceIsZero()
        {
            
            var dto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Rio de Janeiro",
                DepartureTime = DateTime.Now.AddHours(8),
                ArrivalTime = DateTime.Now.AddHours(11),
                BusId = Guid.NewGuid(),
                Price = 0
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.Price)
                .WithErrorMessage("O preço deve ser entre 0.01 e 99999.99");
        }

        [Fact]
        public void Validate_ShouldFail_WhenBusIdIsEmpty()
        {
            
            var dto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Rio de Janeiro",
                DepartureTime = DateTime.Now.AddHours(8),
                ArrivalTime = DateTime.Now.AddHours(11),
                BusId = Guid.Empty,
                Price = 120.00m
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.BusId)
                .WithErrorMessage("O ID do ônibus é obrigatório");
        }
    }
}

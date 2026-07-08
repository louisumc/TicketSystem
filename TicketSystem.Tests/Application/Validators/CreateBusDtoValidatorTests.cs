using FluentAssertions;
using FluentValidation.TestHelper;
using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Application.Validators.Bus;
using Xunit;

namespace TicketSystem.Tests.Application.Validators
{
    public class CreateBusDtoValidatorTests
    {
        private readonly CreateBusDtoValidator _validator;

        public CreateBusDtoValidatorTests()
        {
            _validator = new CreateBusDtoValidator();
        }

        [Fact]
        public void Validate_ShouldPass_WhenAllFieldsAreValid()
        {
            
            var dto = new CreateBusDto
            {
                Plate = "ABC1234",
                Model = "Mercedes Benz O500",
                Company = "Viação Expresso",
                Capacity = 45
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validate_ShouldFail_WhenPlateIsEmpty()
        {
            
            var dto = new CreateBusDto
            {
                Plate = "",
                Model = "Mercedes Benz O500",
                Company = "Viação Expresso",
                Capacity = 45
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.Plate)
                .WithErrorMessage("A placa é obrigatória");
        }

        [Fact]
        public void Validate_ShouldFail_WhenPlateHasInvalidFormat()
        {
            
            var dto = new CreateBusDto
            {
                Plate = "INVALID",
                Model = "Mercedes Benz O500",
                Company = "Viação Expresso",
                Capacity = 45
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.Plate)
                .WithErrorMessage("Formato de placa inválido. Use formato antigo (ABC1234) ou Mercosul (ABC1D23)");
        }

        [Fact]
        public void Validate_ShouldFail_WhenModelIsEmpty()
        {
            
            var dto = new CreateBusDto
            {
                Plate = "ABC1234",
                Model = "",
                Company = "Viação Expresso",
                Capacity = 45
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.Model)
                .WithErrorMessage("O modelo é obrigatório");
        }

        [Fact]
        public void Validate_ShouldFail_WhenCapacityIsZero()
        {
            
            var dto = new CreateBusDto
            {
                Plate = "ABC1234",
                Model = "Mercedes Benz O500",
                Company = "Viação Expresso",
                Capacity = 0
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldHaveValidationErrorFor(x => x.Capacity)
                .WithErrorMessage("A capacidade deve ser entre 1 e 100");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        public void Validate_ShouldPass_WhenCapacityIsInRange(int capacity)
        {
            
            var dto = new CreateBusDto
            {
                Plate = "ABC1234",
                Model = "Mercedes Benz O500",
                Company = "Viação Expresso",
                Capacity = capacity
            };

            
            var result = _validator.TestValidate(dto);

            
            result.ShouldNotHaveValidationErrorFor(x => x.Capacity);
        }
    }
}
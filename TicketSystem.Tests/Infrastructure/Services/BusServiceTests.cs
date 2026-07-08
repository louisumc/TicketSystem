using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Domain.Entities;
using TicketSystem.Infrastructure.Services;
using TicketSystem.Tests.Helpers;
using Xunit;

namespace TicketSystem.Tests.Infrastructure.Services
{
    public class BusServiceTests
    {
        private readonly Mock<IRepository<Bus>> _busRepositoryMock;
        private readonly Mock<IRepository<Trip>> _tripRepositoryMock;
        private readonly IMapper _mapper;
        private readonly BusService _busService;

        public BusServiceTests()
        {
            _busRepositoryMock = new Mock<IRepository<Bus>>();
            _tripRepositoryMock = new Mock<IRepository<Trip>>();
            var loggerFactory = LoggerFactory.Create(builder => { });

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);
            _mapper = config.CreateMapper();

            _busService = new BusService(
            _busRepositoryMock.Object,
            _tripRepositoryMock.Object,
            _mapper);
        }

        [Fact]
        public async Task GetBusResponseByIdAsync_ShouldReturnBus_WhenBusExists()
        {
            
            var bus = TestData.GetValidBus();
            _busRepositoryMock.Setup(x => x.GetByIdAsync(bus.Id))
            .ReturnsAsync(bus);
            _tripRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(new List<Trip>());

            
            var result = await _busService.GetBusResponseByIdAsync(bus.Id);

            
            result.Should().NotBeNull();
            result.Id.Should().Be(bus.Id);
            result.Plate.Should().Be(bus.Plate);
            result.Model.Should().Be(bus.Model);
            result.Company.Should().Be(bus.Company);
            result.Capacity.Should().Be(bus.Capacity);
        }

        [Fact]
        public async Task GetBusResponseByIdAsync_ShouldThrowException_WhenBusNotFound()
        {
            
            var busId = Guid.NewGuid();
            _busRepositoryMock.Setup(x => x.GetByIdAsync(busId))
            .ReturnsAsync((Bus?)null);

            
            Func<Task> act = async () => await _busService.GetBusResponseByIdAsync(busId);

            
            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Ônibus com ID {busId} não encontrado");
        }

        [Fact]
        public async Task GetAllBusResponsesAsync_ShouldReturnAllBuses()
        {
            
            var buses = TestData.GetBusList(3);
            _busRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(buses);
            _tripRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(new List<Trip>());

            
            var result = await _busService.GetAllBusResponsesAsync();

            
            result.Should().HaveCount(3);
            result.Should().AllBeOfType<BusResponseDto>();
        }

        [Fact]
        public async Task GetActiveBusesAsync_ShouldReturnOnlyActiveBuses()
        {
            
            var buses = TestData.GetBusList(3);
            buses[1].IsActive = false;
            _busRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(buses.Where(b => b.IsActive).ToList());

            
            var result = await _busService.GetActiveBusesAsync();

            
            result.Should().AllSatisfy(b => b.IsActive.Should().BeTrue());
        }

        [Fact]
        public async Task CreateBusAsync_ShouldCreateBus_WhenPlateIsUnique()
        {
            
            var createDto = new CreateBusDto
            {
                Plate = "XYZ9999",
                Model = "New Bus",
                Company = "New Company",
                Capacity = 50
            };

            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(false);
            _busRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Bus>()))
            .ReturnsAsync((Bus b) => b);

            
            var result = await _busService.CreateBusAsync(createDto);

            
            result.Should().NotBeNull();
            result.Plate.Should().Be(createDto.Plate);
            result.Model.Should().Be(createDto.Model);
            result.Company.Should().Be(createDto.Company);
            result.Capacity.Should().Be(createDto.Capacity);
        }

        [Fact]
        public async Task CreateBusAsync_ShouldThrowException_WhenPlateAlreadyExists()
        {
            
            var createDto = new CreateBusDto
            {
                Plate = "ABC1234",
                Model = "New Bus",
                Company = "New Company",
                Capacity = 50
            };

            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(true);

            
            Func<Task> act = async () => await _busService.CreateBusAsync(createDto);

            
            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Já existe um ônibus com a placa {createDto.Plate}");
        }

        [Fact]
        public async Task DeleteBusAsync_ShouldSoftDeleteBus_WhenNoActiveTrips()
        {
            
            var bus = TestData.GetValidBus();
            _busRepositoryMock.Setup(x => x.GetByIdAsync(bus.Id))
            .ReturnsAsync(bus);
            _tripRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(new List<Trip>());

            
            await _busService.DeleteBusAsync(bus.Id);

            
            bus.IsActive.Should().BeFalse();
            bus.UpdatedAt.Should().NotBeNull();
            _busRepositoryMock.Verify(x => x.UpdateAsync(bus), Times.Once);
        }

        [Fact]
        public async Task DeleteBusAsync_ShouldThrowException_WhenHasActiveTrips()
        {
            
            var bus = TestData.GetValidBus();
            var trip = TestData.GetValidTrip();
            _busRepositoryMock.Setup(x => x.GetByIdAsync(bus.Id))
            .ReturnsAsync(bus);
            _tripRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(new List<Trip> { trip });

            
            Func<Task> act = async () => await _busService.DeleteBusAsync(bus.Id);

            
            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível excluir um ônibus com viagens ativas");
        }

        [Fact]
        public async Task ExistsByPlateAsync_ShouldReturnTrue_WhenPlateExists()
        {
            
            var plate = "ABC1234";
            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(true);

            
            var result = await _busService.ExistsByPlateAsync(plate);

            
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByPlateAsync_ShouldReturnFalse_WhenPlateNotExists()
        {
            
            var plate = "XYZ9999";
            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(false);

            
            var result = await _busService.ExistsByPlateAsync(plate);

            
            result.Should().BeFalse();
        }
    }
}
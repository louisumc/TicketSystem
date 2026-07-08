using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Services;
using TicketSystem.Tests.Helpers;
using Xunit;

namespace TicketSystem.Tests.Infrastructure.Services
{
    public class TripServiceTests
    {
        private readonly Mock<IRepository<Trip>> _tripRepositoryMock;
        private readonly Mock<IRepository<Bus>> _busRepositoryMock;
        private readonly Mock<IRepository<Seat>> _seatRepositoryMock;
        private readonly Mock<ISeatService> _seatServiceMock;
        private readonly IMapper _mapper;
        private readonly TripService _tripService;

        public TripServiceTests()
        {
            _tripRepositoryMock = new Mock<IRepository<Trip>>();
            _busRepositoryMock = new Mock<IRepository<Bus>>();
            _seatRepositoryMock = new Mock<IRepository<Seat>>();
            _seatServiceMock = new Mock<ISeatService>();

            var loggerFactory = LoggerFactory.Create(builder => { });

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);
            _mapper = config.CreateMapper();

            _tripService = new TripService(
            _tripRepositoryMock.Object,
            _busRepositoryMock.Object,
            _seatRepositoryMock.Object,
            _seatServiceMock.Object,
            _mapper);
        }

        [Fact]
        public async Task GetTripResponseByIdAsync_ShouldReturnTrip_WhenTripExists()
        {
            
            var trip = TestData.GetValidTrip();
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);
            _busRepositoryMock.Setup(x => x.GetByIdAsync(trip.BusId))
            .ReturnsAsync(trip.Bus);

            
            var result = await _tripService.GetTripResponseByIdAsync(trip.Id);

            
            result.Should().NotBeNull();
            result.Id.Should().Be(trip.Id);
            result.Origin.Should().Be(trip.Origin);
            result.Destination.Should().Be(trip.Destination);
            result.Price.Should().Be(trip.Price);
            result.Status.Should().Be(trip.Status);
        }

        [Fact]
        public async Task GetTripResponseByIdAsync_ShouldThrowException_WhenTripNotFound()
        {
            
            var tripId = Guid.NewGuid();
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync((Trip?)null);

            
            Func<Task> act = async () => await _tripService.GetTripResponseByIdAsync(tripId);

            
            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Viagem com ID {tripId} não encontrada");
        }

        [Fact]
        public async Task GetAllTripResponsesAsync_ShouldReturnAllTrips()
        {
            
            var trips = TestData.GetTripList(3);
            _tripRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(trips);
            _busRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(TestData.GetValidBus());

            
            var result = await _tripService.GetAllTripResponsesAsync();

            
            result.Should().HaveCount(3);
            result.Should().AllBeOfType<TripResponseDto>();
        }

        [Fact]
        public async Task GetByBusIdAsync_ShouldReturnTrips_WhenBusExists()
        {
            
            var busId = Guid.NewGuid();
            var trips = TestData.GetTripList(2);
            trips.ForEach(t => t.BusId = busId);

            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(true);
            _tripRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(trips);

            
            var result = await _tripService.GetByBusIdAsync(busId);

            
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(t => t.BusId.Should().Be(busId));
        }

        [Fact]
        public async Task GetByBusIdAsync_ShouldThrowException_WhenBusNotFound()
        {
            
            var busId = Guid.NewGuid();
            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(false);

            
            Func<Task> act = async () => await _tripService.GetByBusIdAsync(busId);

            
            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Ônibus com ID {busId} não encontrado");
        }

        [Fact]
        public async Task CreateTripAsync_ShouldCreateTrip_WhenValid()
        {
            
            var busId = Guid.NewGuid();
            var createDto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Curitiba",
                DepartureTime = DateTime.UtcNow.AddHours(10),
                ArrivalTime = DateTime.UtcNow.AddHours(14),
                BusId = busId,
                Price = 200.00m,
                Status = TripStatus.Scheduled
            };

            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(true);
            _tripRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Trip>()))
            .ReturnsAsync((Trip t) => t);

            
            var result = await _tripService.CreateTripAsync(createDto);

            
            result.Should().NotBeNull();
            result.Origin.Should().Be(createDto.Origin);
            result.Destination.Should().Be(createDto.Destination);
            result.Price.Should().Be(createDto.Price);
            result.Status.Should().Be(createDto.Status);
        }

        [Fact]
        public async Task CreateTripAsync_ShouldThrowException_WhenBusNotFound()
        {
            
            var createDto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Curitiba",
                DepartureTime = DateTime.UtcNow.AddHours(10),
                ArrivalTime = DateTime.UtcNow.AddHours(14),
                BusId = Guid.NewGuid(),
                Price = 200.00m
            };

            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(false);

            
            Func<Task> act = async () => await _tripService.CreateTripAsync(createDto);

            
            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Ônibus com ID {createDto.BusId} não encontrado");
        }

        [Fact]
        public async Task CreateTripWithSeatsAsync_ShouldCreateTripAndGenerateSeats_WhenValid()
        {
            
            var busId = Guid.NewGuid();
            var bus = new Bus { Id = busId, Capacity = 45 };
            var createDto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Curitiba",
                DepartureTime = DateTime.UtcNow.AddHours(10),
                ArrivalTime = DateTime.UtcNow.AddHours(14),
                BusId = busId,
                Price = 200.00m,
                Status = TripStatus.Scheduled
            };

            var createdTrip = new Trip
            {
                Id = Guid.NewGuid(),
                Origin = createDto.Origin,
                Destination = createDto.Destination,
                DepartureTime = createDto.DepartureTime,
                ArrivalTime = createDto.ArrivalTime,
                BusId = busId,
                Price = createDto.Price,
                Status = createDto.Status,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var seats = new List<SeatDto>
{
new SeatDto { Id = Guid.NewGuid(), Number = "1A", Status = SeatStatus.Available },
new SeatDto { Id = Guid.NewGuid(), Number = "1B", Status = SeatStatus.Available }
};

            _busRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Bus, bool>>>()))
            .ReturnsAsync(true);
            _busRepositoryMock.Setup(x => x.GetByIdAsync(busId))
            .ReturnsAsync(bus);
            _tripRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Trip>()))
            .ReturnsAsync(createdTrip);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(createdTrip.Id))
            .ReturnsAsync(createdTrip);
            _seatServiceMock.Setup(x => x.GenerateSeatsForTripAsync(createdTrip.Id, bus.Capacity))
            .ReturnsAsync(seats);
            _seatRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Seat, bool>>>()))
            .ReturnsAsync(new List<Seat>());

            
            var result = await _tripService.CreateTripWithSeatsAsync(createDto);

            
            result.Should().NotBeNull();
            result.Origin.Should().Be(createDto.Origin);
            result.Destination.Should().Be(createDto.Destination);
            result.Price.Should().Be(createDto.Price);
            result.Status.Should().Be(createDto.Status);
            _seatServiceMock.Verify(x => x.GenerateSeatsForTripAsync(createdTrip.Id, bus.Capacity), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldUpdateStatus_WhenTripExistsAndValid()
        {
            
            var trip = TestData.GetValidTrip();
            var newStatus = TripStatus.InProgress;

            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);

            
            await _tripService.UpdateStatusAsync(trip.Id, newStatus);

            
            trip.Status.Should().Be(newStatus);
            _tripRepositoryMock.Verify(x => x.UpdateAsync(trip), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldThrowException_WhenTripNotFound()
        {
            
            var tripId = Guid.NewGuid();
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync((Trip?)null);

            
            Func<Task> act = async () => await _tripService.UpdateStatusAsync(tripId, TripStatus.InProgress);

            
            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Viagem com ID {tripId} não encontrada");
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldThrowException_WhenTripIsCompleted()
        {
            
            var trip = TestData.GetValidTrip();
            trip.Status = TripStatus.Completed;

            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);

            
            Func<Task> act = async () => await _tripService.UpdateStatusAsync(trip.Id, TripStatus.InProgress);

            
            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível alterar o status de uma viagem já concluída");
        }

        [Fact]
        public async Task UpdateStatusAsync_ShouldThrowException_WhenTripIsCancelled()
        {
            
            var trip = TestData.GetValidTrip();
            trip.Status = TripStatus.Cancelled;

            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);

            
            Func<Task> act = async () => await _tripService.UpdateStatusAsync(trip.Id, TripStatus.InProgress);

            
            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível alterar o status de uma viagem já cancelada");
        }

        [Fact]
        public async Task DeleteTripAsync_ShouldSoftDeleteTrip_WhenTripExists()
        {
            
            var trip = TestData.GetValidTrip();
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);

            
            await _tripService.DeleteTripAsync(trip.Id);

            
            trip.IsActive.Should().BeFalse();
            _tripRepositoryMock.Verify(x => x.UpdateAsync(trip), Times.Once);
        }

        [Fact]
        public async Task DeleteTripAsync_ShouldThrowException_WhenTripNotFound()
        {
            
            var tripId = Guid.NewGuid();
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync((Trip?)null);

            
            Func<Task> act = async () => await _tripService.DeleteTripAsync(tripId);

            
            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Viagem com ID {tripId} não encontrada");
        }

        [Fact]
        public async Task GetTripDetailsByIdAsync_ShouldReturnTripWithSeats_WhenTripExists()
        {
            
            var trip = TestData.GetValidTrip();
            var seats = new List<Seat>
{
new Seat { Id = Guid.NewGuid(), Number = "1A", Status = SeatStatus.Available, Row = 1, Column = 1, IsActive = true },
new Seat { Id = Guid.NewGuid(), Number = "1B", Status = SeatStatus.Available, Row = 1, Column = 2, IsActive = true }
};
            trip.Seats = seats;

            _tripRepositoryMock.Setup(x => x.GetByIdAsync(trip.Id))
            .ReturnsAsync(trip);
            _busRepositoryMock.Setup(x => x.GetByIdAsync(trip.BusId))
            .ReturnsAsync(trip.Bus);
            _seatRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Seat, bool>>>()))
            .ReturnsAsync(seats);

            
            var result = await _tripService.GetTripDetailsByIdAsync(trip.Id);

            
            result.Should().NotBeNull();
            result.Id.Should().Be(trip.Id);
            result.Origin.Should().Be(trip.Origin);
            result.Destination.Should().Be(trip.Destination);
            result.Seats.Should().HaveCount(2);
            result.TotalSeats.Should().Be(2);
            result.AvailableSeats.Should().Be(2);
        }

        [Fact]
        public async Task GetTripDetailsByIdAsync_ShouldThrowException_WhenTripNotFound()
        {
            
            var tripId = Guid.NewGuid();
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync((Trip?)null);

            
            Func<Task> act = async () => await _tripService.GetTripDetailsByIdAsync(tripId);

            
            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Viagem com ID {tripId} não encontrada");
        }
    }
}
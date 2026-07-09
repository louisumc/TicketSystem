using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Services;
using TicketSystem.Tests.Helpers;
using Xunit;

namespace TicketSystem.Tests.Infrastructure.Services
{
    public class SeatServiceTests
    {
        private readonly Mock<IRepository<Seat>> _seatRepositoryMock;
        private readonly Mock<IRepository<Trip>> _tripRepositoryMock;
        private readonly IMapper _mapper;
        private readonly SeatService _seatService;

        public SeatServiceTests()
        {
            _seatRepositoryMock = new Mock<IRepository<Seat>>();
            _tripRepositoryMock = new Mock<IRepository<Trip>>();

            var loggerFactory = LoggerFactory.Create(builder => { });

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);
            _mapper = config.CreateMapper();

            _seatService = new SeatService(
            _seatRepositoryMock.Object,
            _tripRepositoryMock.Object,
            _mapper);
        }

        [Fact]
        public async Task GetSeatByIdAsync_ShouldReturnSeat_WhenSeatExists()
        {
            var seatId = Guid.NewGuid();
            var seat = new Seat
            {
                Id = seatId,
                Number = "1A",
                Status = SeatStatus.Available,
                IsActive = true
            };

            _seatRepositoryMock.Setup(x => x.GetByIdAsync(seatId))
            .ReturnsAsync(seat);

            var result = await _seatService.GetSeatByIdAsync(seatId);

            result.Should().NotBeNull();
            result.Id.Should().Be(seatId);
            result.Number.Should().Be("1A");
            result.Status.Should().Be(SeatStatus.Available);
        }

        [Fact]
        public async Task GetSeatByIdAsync_ShouldThrowException_WhenSeatNotFound()
        {
            var seatId = Guid.NewGuid();

            _seatRepositoryMock.Setup(x => x.GetByIdAsync(seatId))
            .ReturnsAsync((Seat)null);

            Func<Task> act = async () => await _seatService.GetSeatByIdAsync(seatId);

            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Assento com ID {seatId} não encontrado");
        }

        [Fact]
        public async Task GetSeatsByTripIdAsync_ShouldReturnSeats_WhenTripExists()
        {
            var tripId = Guid.NewGuid();
            var seats = new List<Seat>
{
new Seat { Id = Guid.NewGuid(), TripId = tripId, Number = "1A", Status = SeatStatus.Available, Row = 1, Column = 1, IsActive = true },
new Seat { Id = Guid.NewGuid(), TripId = tripId, Number = "1B", Status = SeatStatus.Available, Row = 1, Column = 2, IsActive = true }
};

            _tripRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(true);
            _seatRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Seat, bool>>>()))
            .ReturnsAsync(seats);

            var result = await _seatService.GetSeatsByTripIdAsync(tripId);

            result.Should().HaveCount(2);
            result.Should().AllSatisfy(s => s.TripId.Should().Be(tripId));
        }

        [Fact]
        public async Task GetSeatsByTripIdAsync_ShouldThrowException_WhenTripNotFound()
        {
            var tripId = Guid.NewGuid();

            _tripRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(false);

            Func<Task> act = async () => await _seatService.GetSeatsByTripIdAsync(tripId);

            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Viagem com ID {tripId} não encontrada");
        }

        [Fact]
        public async Task CreateSeatAsync_ShouldCreateSeat_WhenValid()
        {
            var tripId = Guid.NewGuid();
            var createDto = new CreateSeatDto
            {
                TripId = tripId,
                Number = "2A",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 2,
                Column = 1,
                PriceMultiplier = 1.10m
            };

            _tripRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(true);
            _seatRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Seat, bool>>>()))
            .ReturnsAsync(false);
            _seatRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Seat>()))
            .ReturnsAsync((Seat s) => s);

            var result = await _seatService.CreateSeatAsync(createDto);

            result.Should().NotBeNull();
            result.Number.Should().Be("2A");
            result.Type.Should().Be(SeatType.Window);
            result.Status.Should().Be(SeatStatus.Available);
        }

        [Fact]
        public async Task CreateSeatAsync_ShouldThrowException_WhenSeatNumberAlreadyExists()
        {
            var tripId = Guid.NewGuid();
            var createDto = new CreateSeatDto
            {
                TripId = tripId,
                Number = "1A",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 1,
                Column = 1
            };

            _tripRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Trip, bool>>>()))
            .ReturnsAsync(true);
            _seatRepositoryMock.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Seat, bool>>>()))
            .ReturnsAsync(true);

            Func<Task> act = async () => await _seatService.CreateSeatAsync(createDto);

            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Assento {createDto.Number} já existe nesta viagem");
        }

        [Fact]
        public async Task GenerateSeatsForTripAsync_ShouldGenerateSeats_WhenValid()
        {
            var tripId = Guid.NewGuid();
            var capacity = 45;
            var trip = new Trip { Id = tripId };

            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync(trip);
            _seatRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Seat, bool>>>()))
            .ReturnsAsync(new List<Seat>());

            var result = await _seatService.GenerateSeatsForTripAsync(tripId, capacity);

            result.Should().HaveCount(capacity);
            result.Should().AllSatisfy(s =>
            {
                s.TripId.Should().Be(tripId);
                s.Status.Should().Be(SeatStatus.Available);
                s.IsActive.Should().BeTrue();
            });
        }

        [Fact]
        public async Task GenerateSeatsForTripAsync_ShouldThrowException_WhenSeatsAlreadyExist()
        {
            var tripId = Guid.NewGuid();
            var capacity = 45;
            var trip = new Trip { Id = tripId };
            var existingSeats = new List<Seat> { new Seat { Id = Guid.NewGuid(), TripId = tripId } };

            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync(trip);
            _seatRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Seat, bool>>>()))
            .ReturnsAsync(existingSeats);

            Func<Task> act = async () => await _seatService.GenerateSeatsForTripAsync(tripId, capacity);

            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Esta viagem já possui assentos gerados");
        }

        [Fact]
        public async Task UpdateSeatStatusAsync_ShouldUpdateStatus_WhenValid()
        {
            var seatId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var seat = new Seat
            {
                Id = seatId,
                TripId = tripId,
                Number = "1A",
                Status = SeatStatus.Available
            };
            var trip = new Trip
            {
                Id = tripId,
                Status = TripStatus.Scheduled
            };
            var updateDto = new UpdateSeatStatusDto
            {
                Status = SeatStatus.Reserved,
                PassengerName = "João Silva",
                PassengerDocument = "12345678901"
            };

            _seatRepositoryMock.Setup(x => x.GetByIdAsync(seatId))
            .ReturnsAsync(seat);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync(trip);

            var result = await _seatService.UpdateSeatStatusAsync(seatId, updateDto);

            result.Status.Should().Be(SeatStatus.Reserved);
            result.PassengerName.Should().Be("João Silva");
            result.PassengerDocument.Should().Be("12345678901");
        }

        [Fact]
        public async Task UpdateSeatStatusAsync_ShouldThrowException_WhenTripIsCompleted()
        {
            var seatId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var seat = new Seat
            {
                Id = seatId,
                TripId = tripId,
                Number = "1A",
                Status = SeatStatus.Available
            };
            var trip = new Trip
            {
                Id = tripId,
                Status = TripStatus.Completed
            };
            var updateDto = new UpdateSeatStatusDto
            {
                Status = SeatStatus.Reserved
            };

            _seatRepositoryMock.Setup(x => x.GetByIdAsync(seatId))
            .ReturnsAsync(seat);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync(trip);

            Func<Task> act = async () => await _seatService.UpdateSeatStatusAsync(seatId, updateDto);

            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível alterar assento de uma viagem já concluída");
        }

        [Fact]
        public async Task UpdateSeatStatusAsync_ShouldThrowException_WhenSeatIsSold()
        {
            var seatId = Guid.NewGuid();
            var tripId = Guid.NewGuid();
            var seat = new Seat
            {
                Id = seatId,
                TripId = tripId,
                Number = "1A",
                Status = SeatStatus.Sold
            };
            var trip = new Trip
            {
                Id = tripId,
                Status = TripStatus.Scheduled
            };
            var updateDto = new UpdateSeatStatusDto
            {
                Status = SeatStatus.Reserved
            };

            _seatRepositoryMock.Setup(x => x.GetByIdAsync(seatId))
            .ReturnsAsync(seat);
            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync(trip);

            Func<Task> act = async () => await _seatService.UpdateSeatStatusAsync(seatId, updateDto);

            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Não é possível alterar um assento já vendido");
        }
    }
}

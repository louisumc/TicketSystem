using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Infrastructure.Services;
using TicketSystem.Tests.Helpers;
using Xunit;

namespace TicketSystem.Tests.Infrastructure.Services
{
    public class ReservationServiceTests
    {
        private readonly Mock<IRepository<Reservation>> _reservationRepositoryMock;
        private readonly Mock<ApplicationDbContext> _contextMock;
        private readonly Mock<IRepository<Trip>> _tripRepositoryMock;
        private readonly Mock<IRepository<Seat>> _seatRepositoryMock;
        private readonly Mock<IRepository<Passenger>> _passengerRepositoryMock;
        private readonly Mock<IRepository<ReservationSeat>> _reservationSeatRepositoryMock;
        private readonly Mock<IPassengerService> _passengerServiceMock;
        private readonly IMapper _mapper;
        private readonly ReservationService _reservationService;

        public ReservationServiceTests()
        {
            _reservationRepositoryMock = new Mock<IRepository<Reservation>>();
            _contextMock = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            _tripRepositoryMock = new Mock<IRepository<Trip>>();
            _seatRepositoryMock = new Mock<IRepository<Seat>>();
            _passengerRepositoryMock = new Mock<IRepository<Passenger>>();
            _reservationSeatRepositoryMock = new Mock<IRepository<ReservationSeat>>();
            _passengerServiceMock = new Mock<IPassengerService>();

            var loggerFactory = LoggerFactory.Create(builder => { });

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);
            _mapper = config.CreateMapper();

            _reservationService = new ReservationService(
            _reservationRepositoryMock.Object,
            _contextMock.Object,
            _tripRepositoryMock.Object,
            _seatRepositoryMock.Object,
            _passengerRepositoryMock.Object,
            _reservationSeatRepositoryMock.Object,
            _passengerServiceMock.Object,
            _mapper);
        }

        [Fact]
        public async Task GetReservationByIdAsync_ShouldReturnReservation_WhenExists()
        {
            var reservationId = Guid.NewGuid();
            var reservation = new Reservation
            {
                Id = reservationId,
                Status = ReservationStatus.Pending,
                TotalAmount = 120.00m,
                IsActive = true
            };

            _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservationId))
            .ReturnsAsync(reservation);

            var result = await _reservationService.GetReservationByIdAsync(reservationId);

            result.Should().NotBeNull();
            result.Id.Should().Be(reservationId);
            result.Status.Should().Be(ReservationStatus.Pending);
        }

        [Fact]
        public async Task GetReservationByIdAsync_ShouldThrowException_WhenNotFound()
        {
            var reservationId = Guid.NewGuid();

            _reservationRepositoryMock.Setup(x => x.GetByIdAsync(reservationId))
            .ReturnsAsync((Reservation)null);

            Func<Task> act = async () => await _reservationService.GetReservationByIdAsync(reservationId);

            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Reserva com ID {reservationId} não encontrada");
        }

        [Fact]
        public async Task GetAvailableSeatsAsync_ShouldReturnAvailableSeats_WhenTripExists()
        {
            var tripId = Guid.NewGuid();
            var trip = new Trip
            {
                Id = tripId,
                Origin = "São Paulo",
                Destination = "Rio de Janeiro",
                DepartureTime = DateTime.UtcNow.AddDays(1),
                Bus = new Bus { Capacity = 45 }
            };
            var seats = new List<Seat>
{
new Seat { Id = Guid.NewGuid(), TripId = tripId, Number = "1A", Status = SeatStatus.Available, Row = 1, Column = 1, IsActive = true },
new Seat { Id = Guid.NewGuid(), TripId = tripId, Number = "1B", Status = SeatStatus.Reserved, Row = 1, Column = 2, IsActive = true },
new Seat { Id = Guid.NewGuid(), TripId = tripId, Number = "1C", Status = SeatStatus.Sold, Row = 1, Column = 3, IsActive = true }
};

            _tripRepositoryMock.Setup(x => x.GetByIdAsync(tripId))
            .ReturnsAsync(trip);
            _seatRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Seat, bool>>>()))
            .ReturnsAsync(seats);

            var result = await _reservationService.GetAvailableSeatsAsync(tripId);

            result.Should().NotBeNull();
            result.TripId.Should().Be(tripId);
            result.TotalSeats.Should().Be(3);
            result.AvailableSeats.Should().Be(1);
            result.ReservedSeats.Should().Be(1);
            result.SoldSeats.Should().Be(1);
        }
    }
}

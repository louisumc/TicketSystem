using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Services;
using TicketSystem.Tests.Helpers;
using Xunit;

namespace TicketSystem.Tests.Infrastructure.Services
{
    public class PassengerServiceTests
    {
        private readonly Mock<IRepository<Passenger>> _passengerRepositoryMock;
        private readonly Mock<IRepository<Reservation>> _reservationRepositoryMock;
        private readonly IMapper _mapper;
        private readonly PassengerService _passengerService;

        public PassengerServiceTests()
        {
            _passengerRepositoryMock = new Mock<IRepository<Passenger>>();
            _reservationRepositoryMock = new Mock<IRepository<Reservation>>();

            var loggerFactory = LoggerFactory.Create(builder => { });

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);
            _mapper = config.CreateMapper();

            _passengerService = new PassengerService(
            _passengerRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _mapper);
        }

        [Fact]
        public async Task GetOrCreatePassengerAsync_ShouldReturnExistingPassenger_WhenExists()
        {
            var document = "12345678901";
            var existingPassenger = new Passenger
            {
                Id = Guid.NewGuid(),
                Name = "João Silva",
                Document = document,
                Email = "joao@email.com",
                Phone = "11999999999"
            };
            var passengerInfo = new PassengerInfoDto
            {
                Name = "João Silva Atualizado",
                Document = document,
                Email = "joao.novo@email.com",
                Phone = "11888888888"
            };

            _passengerRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Passenger, bool>>>()))
            .ReturnsAsync(new List<Passenger> { existingPassenger });

            var result = await _passengerService.GetOrCreatePassengerAsync(passengerInfo);

            result.Should().NotBeNull();
            result.Id.Should().Be(existingPassenger.Id);
            result.Name.Should().Be("João Silva Atualizado");
            result.Email.Should().Be("joao.novo@email.com");
            result.Phone.Should().Be("11888888888");
            _passengerRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Passenger>()), Times.Once);
        }

        [Fact]
        public async Task GetOrCreatePassengerAsync_ShouldCreateNewPassenger_WhenNotExists()
        {
            var document = "12345678901";
            var passengerInfo = new PassengerInfoDto
            {
                Name = "João Silva",
                Document = document,
                Email = "joao@email.com",
                Phone = "11999999999"
            };

            _passengerRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Passenger, bool>>>()))
            .ReturnsAsync(new List<Passenger>());
            _passengerRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Passenger>()))
            .ReturnsAsync((Passenger p) => p);

            var result = await _passengerService.GetOrCreatePassengerAsync(passengerInfo);

            result.Should().NotBeNull();
            result.Name.Should().Be("João Silva");
            result.Document.Should().Be(document);
            result.Email.Should().Be("joao@email.com");
            result.Phone.Should().Be("11999999999");
            _passengerRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Passenger>()), Times.Once);
        }

        [Fact]
        public async Task GetPassengerByDocumentAsync_ShouldReturnPassenger_WhenExists()
        {
            var document = "12345678901";
            var passenger = new Passenger
            {
                Id = Guid.NewGuid(),
                Name = "João Silva",
                Document = document,
                Email = "joao@email.com",
                Phone = "11999999999"
            };

            _passengerRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Passenger, bool>>>()))
            .ReturnsAsync(new List<Passenger> { passenger });

            var result = await _passengerService.GetPassengerByDocumentAsync(document);

            result.Should().NotBeNull();
            result.Document.Should().Be(document);
        }

        [Fact]
        public async Task GetPassengerByDocumentAsync_ShouldThrowException_WhenNotFound()
        {
            var document = "12345678901";

            _passengerRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Passenger, bool>>>()))
            .ReturnsAsync(new List<Passenger>());

            Func<Task> act = async () => await _passengerService.GetPassengerByDocumentAsync(document);

            await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Passageiro com CPF {document} não encontrado");
        }

        [Fact]
        public async Task HasPendingReservationForTripAsync_ShouldReturnTrue_WhenHasPendingReservation()
        {
            var document = "12345678901";
            var tripId = Guid.NewGuid();
            var passenger = new Passenger
            {
                Id = Guid.NewGuid(),
                Document = document
            };
            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),
                PassengerId = passenger.Id,
                TripId = tripId,
                Status = ReservationStatus.Pending,
                ExpiresAt = DateTime.Now.AddMinutes(10)
            };

            _passengerRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Passenger, bool>>>()))
            .ReturnsAsync(new List<Passenger> { passenger });
            _reservationRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Reservation, bool>>>()))
            .ReturnsAsync(new List<Reservation> { reservation });

            var result = await _passengerService.HasPendingReservationForTripAsync(document, tripId);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasPendingReservationForTripAsync_ShouldReturnFalse_WhenNoPendingReservation()
        {
            var document = "12345678901";
            var tripId = Guid.NewGuid();
            var passenger = new Passenger
            {
                Id = Guid.NewGuid(),
                Document = document
            };

            _passengerRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Passenger, bool>>>()))
            .ReturnsAsync(new List<Passenger> { passenger });
            _reservationRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Reservation, bool>>>()))
            .ReturnsAsync(new List<Reservation>());

            var result = await _passengerService.HasPendingReservationForTripAsync(document, tripId);

            result.Should().BeFalse();
        }
    }
}


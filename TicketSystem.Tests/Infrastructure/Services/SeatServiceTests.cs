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
        public async Task GenerateSeatsForTripAsync_ShouldGenerateCorrectNumberOfSeats()
        {
            
            var tripId = Guid.NewGuid();
            var capacity = 40;
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
            var capacity = 40;
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
    }
}

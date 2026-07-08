using FluentAssertions;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Tests.Helpers;
using Xunit;

namespace TicketSystem.Tests.Domain
{
    public class TripTests
    {
        [Fact]
        public void Trip_ShouldBeCreated_WithValidProperties()
        {
            
            var trip = TestData.GetValidTrip();

            
            trip.Should().NotBeNull();
            trip.Id.Should().NotBeEmpty();
            trip.Origin.Should().Be("São Paulo");
            trip.Destination.Should().Be("Rio de Janeiro");
            trip.Price.Should().Be(120.00m);
            trip.Status.Should().Be(TripStatus.Scheduled);
            trip.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Trip_ShouldHaveDefaultValues_WhenCreated()
        {
            
            var trip = new Trip();

           
            trip.Id.Should().NotBeEmpty();
            trip.IsActive.Should().BeTrue();
            trip.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            trip.Status.Should().Be(TripStatus.Scheduled);
            trip.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Trip_ShouldUpdateStatus_WhenChanged()
        {
            
            var trip = TestData.GetValidTrip();

            
            trip.Status = TripStatus.InProgress;

            
            trip.Status.Should().Be(TripStatus.InProgress);
        }

        [Fact]
        public void Trip_ShouldRequireBus()
        {
            
            var trip = new Trip();

            
            trip.BusId.Should().BeEmpty();
            trip.Bus.Should().BeNull();
        }

        [Fact]
        public void Trip_ShouldHaveValidDateRange()
        {
            
            var trip = TestData.GetValidTrip();

            
            trip.DepartureTime.Should().BeBefore(trip.ArrivalTime);
            trip.DepartureTime.Should().BeAfter(DateTime.UtcNow);
        }
    }
}
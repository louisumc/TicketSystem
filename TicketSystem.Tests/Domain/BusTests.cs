using FluentAssertions;
using TicketSystem.Domain.Entities;
using TicketSystem.Tests.Helpers;
using Xunit;

namespace TicketSystem.Tests.Domain
{
    public class BusTests
    {
        [Fact]
        public void Bus_ShouldBeCreated_WithValidProperties()
        {
            
            var bus = TestData.GetValidBus();

             
            bus.Should().NotBeNull();
            bus.Id.Should().NotBeEmpty();
            bus.Plate.Should().Be("ABC1234");
            bus.Model.Should().Be("Mercedes Benz O500");
            bus.Company.Should().Be("Viação Expresso");
            bus.Capacity.Should().Be(45);
            bus.IsActive.Should().BeTrue();
            bus.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Bus_ShouldHaveDefaultValues_WhenCreated()
        {
            
            var bus = new Bus();

             
            bus.Id.Should().NotBeEmpty();
            bus.IsActive.Should().BeTrue();
            bus.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            bus.UpdatedAt.Should().BeNull();
            bus.Trips.Should().BeEmpty();
        }

        [Fact]
        public void Bus_ShouldUpdateUpdatedAt_WhenModified()
        {
            
            var bus = TestData.GetValidBus();
            var originalDate = bus.CreatedAt;

            
            bus.UpdatedAt = DateTime.Now;
            bus.IsActive = false;

            
            bus.UpdatedAt.Should().NotBeNull();
            bus.UpdatedAt.Value.Should().BeAfter(originalDate);
            bus.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Bus_ShouldAllowAddingTrips()
        {
            
            var bus = TestData.GetValidBus();
            var trip1 = TestData.GetValidTrip();
            var trip2 = TestData.GetValidTrip();

            
            bus.Trips.Add(trip1);
            bus.Trips.Add(trip2);

            
            bus.Trips.Should().HaveCount(2);
            bus.Trips.Should().Contain(trip1);
            bus.Trips.Should().Contain(trip2);
        }
    }
}
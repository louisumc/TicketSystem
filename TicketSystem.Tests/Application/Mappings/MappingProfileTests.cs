using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Application.Mappings;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Tests.Helpers;

namespace TicketSystem.Tests.Application.Mappings
{
    public class MappingProfileTests
    {
        private readonly IMapper _mapper;

        public MappingProfileTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void MappingProfile_ShouldBeValid()
        {

            var loggerFactory = LoggerFactory.Create(builder => { });

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            }, loggerFactory);

            
            config.AssertConfigurationIsValid();
        }

        [Fact]
        public void Map_BusToBusResponseDto_ShouldMapCorrectly()
        {
            
            var bus = TestData.GetValidBus();
            bus.Trips.Add(TestData.GetValidTrip());
            bus.Trips.Add(TestData.GetValidTrip());

            
            var dto = _mapper.Map<BusResponseDto>(bus);

            
            dto.Should().NotBeNull();
            dto.Id.Should().Be(bus.Id);
            dto.Plate.Should().Be(bus.Plate);
            dto.Model.Should().Be(bus.Model);
            dto.Company.Should().Be(bus.Company);
            dto.Capacity.Should().Be(bus.Capacity);
            dto.CreatedAt.Should().Be(bus.CreatedAt);
            dto.UpdatedAt.Should().Be(bus.UpdatedAt);
            dto.IsActive.Should().Be(bus.IsActive);
            dto.TotalTrips.Should().Be(bus.Trips.Count);
        }

        [Fact]
        public void Map_CreateBusDtoToBus_ShouldMapCorrectly()
        {
            
            var dto = new CreateBusDto
            {
                Plate = "XYZ9999",
                Model = "New Model",
                Company = "New Company",
                Capacity = 50
            };

            
            var bus = _mapper.Map<Bus>(dto);

            
            bus.Should().NotBeNull();
            bus.Plate.Should().Be(dto.Plate);
            bus.Model.Should().Be(dto.Model);
            bus.Company.Should().Be(dto.Company);
            bus.Capacity.Should().Be(dto.Capacity);
        }

        [Fact]
        public void Map_UpdateBusDtoToBus_ShouldMapCorrectly()
        {
            
            var busId = Guid.NewGuid();
            var dto = new UpdateBusDto
            {
                Id = busId,
                Plate = "XYZ9999",
                Model = "Updated Model",
                Company = "Updated Company",
                Capacity = 55,
                IsActive = true
            };

            
            var bus = _mapper.Map<Bus>(dto);

            
            bus.Should().NotBeNull();
            bus.Id.Should().Be(dto.Id);
            bus.Plate.Should().Be(dto.Plate);
            bus.Model.Should().Be(dto.Model);
            bus.Company.Should().Be(dto.Company);
            bus.Capacity.Should().Be(dto.Capacity);
            bus.IsActive.Should().Be(dto.IsActive);
        }

        [Fact]
        public void Map_TripToTripResponseDto_ShouldMapCorrectly()
        {
            
            var trip = TestData.GetValidTrip();

            
            var dto = _mapper.Map<TripResponseDto>(trip);

            
            dto.Should().NotBeNull();
            dto.Id.Should().Be(trip.Id);
            dto.Origin.Should().Be(trip.Origin);
            dto.Destination.Should().Be(trip.Destination);
            dto.DepartureTime.Should().Be(trip.DepartureTime);
            dto.ArrivalTime.Should().Be(trip.ArrivalTime);
            dto.BusId.Should().Be(trip.BusId);
            dto.Price.Should().Be(trip.Price);
            dto.Status.Should().Be(trip.Status);
            dto.CreatedAt.Should().Be(trip.CreatedAt);
            dto.UpdatedAt.Should().Be(trip.UpdatedAt);
            dto.IsActive.Should().Be(trip.IsActive);
            dto.BusPlate.Should().Be(trip.Bus.Plate);
            dto.BusModel.Should().Be(trip.Bus.Model);
            dto.BusCompany.Should().Be(trip.Bus.Company);
        }

        [Fact]
        public void Map_CreateTripDtoToTrip_ShouldMapCorrectly()
        {
            
            var busId = Guid.NewGuid();
            var dto = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Maringá",
                DepartureTime = DateTime.UtcNow.AddHours(10),
                ArrivalTime = DateTime.UtcNow.AddHours(14),
                BusId = busId,
                Price = 200.00m,
                Status = TripStatus.Scheduled
            };

            
            var trip = _mapper.Map<Trip>(dto);

            
            trip.Should().NotBeNull();
            trip.Origin.Should().Be(dto.Origin);
            trip.Destination.Should().Be(dto.Destination);
            trip.DepartureTime.Should().Be(dto.DepartureTime);
            trip.ArrivalTime.Should().Be(dto.ArrivalTime);
            trip.BusId.Should().Be(dto.BusId);
            trip.Price.Should().Be(dto.Price);
            trip.Status.Should().Be(dto.Status);
        }

        [Fact]
        public void Map_UpdateTripDtoToTrip_ShouldMapCorrectly()
        {
            
            var tripId = Guid.NewGuid();
            var busId = Guid.NewGuid();
            var dto = new UpdateTripDto
            {
                Id = tripId,
                Origin = "Updated Origin",
                Destination = "Updated Destination",
                DepartureTime = DateTime.UtcNow.AddHours(12),
                ArrivalTime = DateTime.UtcNow.AddHours(16),
                BusId = busId,
                Price = 250.00m,
                Status = TripStatus.InProgress,
                IsActive = true
            };

            
            var trip = _mapper.Map<Trip>(dto);

            
            trip.Should().NotBeNull();
            trip.Id.Should().Be(dto.Id);
            trip.Origin.Should().Be(dto.Origin);
            trip.Destination.Should().Be(dto.Destination);
            trip.DepartureTime.Should().Be(dto.DepartureTime);
            trip.ArrivalTime.Should().Be(dto.ArrivalTime);
            trip.BusId.Should().Be(dto.BusId);
            trip.Price.Should().Be(dto.Price);
            trip.Status.Should().Be(dto.Status);
            trip.IsActive.Should().Be(dto.IsActive);
        }
    }
}
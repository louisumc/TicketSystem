using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Application.Responses;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Data;
using Xunit;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.TestHost;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Infrastructure.Services;
using AutoMapper;
using Microsoft.Extensions.Logging;
using TicketSystem.Api.Controllers;
using Microsoft.AspNetCore.Builder;

namespace TicketSystem.Tests.Api.Controllers
{
    public class ReservationsControllerIntegrationTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHost _host;
        private readonly string _databaseName;

        public ReservationsControllerIntegrationTests()
        {
            _databaseName = $"TicketSystemTestDb{Guid.NewGuid():N}";

            _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
    .UseTestServer()
    .ConfigureServices(services =>
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                var loggerFactory = LoggerFactory.Create(builder => { });
                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<MappingProfile>();
                }, loggerFactory);
                services.AddSingleton(config.CreateMapper());

                services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
                services.AddScoped<IBusService, BusService>();
                services.AddScoped<ITripService, TripService>();
                services.AddScoped<ISeatService, SeatService>();
                services.AddScoped<IPassengerService, PassengerService>();
                services.AddScoped<IReservationService, ReservationService>();

                services.AddControllers()
    .AddApplicationPart(typeof(ReservationsController).Assembly);
            })
    .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            });
            })
            .Start();

            _client = _host.GetTestClient();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var scope = _host.Services.CreateScope();
            _dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            _dbContext.Reservations.RemoveRange(_dbContext.Reservations);
            _dbContext.ReservationSeats.RemoveRange(_dbContext.ReservationSeats);
            _dbContext.Seats.RemoveRange(_dbContext.Seats);
            _dbContext.Trips.RemoveRange(_dbContext.Trips);
            _dbContext.Buses.RemoveRange(_dbContext.Buses);
            _dbContext.Passengers.RemoveRange(_dbContext.Passengers);
            _dbContext.SaveChanges();

            var now = DateTime.Now;

            var bus = new Bus
            {
                Id = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA"),
                Plate = "ABC1234",
                Model = "Mercedes Benz O500",
                Company = "Viação Expresso",
                Capacity = 45,
                IsActive = true,
                CreatedAt = now
            };

            _dbContext.Buses.Add(bus);
            _dbContext.SaveChanges();

            var trip = new Trip
            {
                Id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
                Origin = "São Paulo",
                Destination = "Rio de Janeiro",
                DepartureTime = now.AddDays(2).AddHours(8),
                ArrivalTime = now.AddDays(2).AddHours(11),
                BusId = bus.Id,
                Price = 120.00m,
                Status = TripStatus.Scheduled,
                IsActive = true,
                CreatedAt = now
            };

            _dbContext.Trips.Add(trip);
            _dbContext.SaveChanges();

            var passenger = new Passenger
            {
                Id = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"),
                Name = "João Silva",
                Document = "12345678901",
                Email = "joao@email.com",
                Phone = "11999999999",
                IsActive = true,
                CreatedAt = now
            };

            _dbContext.Passengers.Add(passenger);
            _dbContext.SaveChanges();

            var seats = new List<Seat>
{
new Seat
{
Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
TripId = trip.Id,
Number = "1A",
Type = SeatType.Window,
Status = SeatStatus.Available,
Row = 1,
Column = 1,
PriceMultiplier = 1.10m,
IsActive = true,
CreatedAt = now
},
new Seat
{
Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
TripId = trip.Id,
Number = "1B",
Type = SeatType.Aisle,
Status = SeatStatus.Available,
Row = 1,
Column = 2,
PriceMultiplier = 1.05m,
IsActive = true,
CreatedAt = now
},
new Seat
{
Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
TripId = trip.Id,
Number = "1C",
Type = SeatType.Middle,
Status = SeatStatus.Reserved,
Row = 1,
Column = 3,
PriceMultiplier = 1.00m,
IsActive = true,
CreatedAt = now
}
};

            _dbContext.Seats.AddRange(seats);
            _dbContext.SaveChanges();

            var reservation = new Reservation
            {
                Id = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD"),
                TripId = trip.Id,
                PassengerId = passenger.Id,
                ReservationDate = now,
                ExpiresAt = now.AddMinutes(15),
                Status = ReservationStatus.Pending,
                TotalAmount = 126.00m,
                IsActive = true,
                CreatedAt = now
            };

            _dbContext.Reservations.Add(reservation);
            _dbContext.SaveChanges();

            var reservationSeat = new ReservationSeat
            {
                Id = Guid.Parse("EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE"),
                ReservationId = reservation.Id,
                SeatId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Price = 126.00m,
                IsActive = true,
                CreatedAt = now
            };

            _dbContext.ReservationSeats.Add(reservationSeat);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Create_ShouldCreateReservation_WhenValid()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var createDto = new CreateReservationDto
            {
                TripId = tripId,
                Passenger = new PassengerInfoDto
                {
                    Name = "Maria Santos",
                    Document = "98765432100",
                    Email = "maria@email.com",
                    Phone = "11888888888"
                },
                SeatNumbers = new List<string> { "1A" }
            };

            var content = new StringContent(
            JsonSerializer.Serialize(createDto, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/reservations", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Status.Should().Be(ReservationStatus.Pending);
            result.Data.TotalAmount.Should().Be(132.00m);
            result.Data.Seats.Should().HaveCount(1);
            result.Data.Seats.First().SeatNumber.Should().Be("1A");
            result.Message.Should().Be("Reserva criada com sucesso");
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenSeatNotAvailable()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var createDto = new CreateReservationDto
            {
                TripId = tripId,
                Passenger = new PassengerInfoDto
                {
                    Name = "Maria Santos",
                    Document = "98765432100",
                    Email = "maria@email.com",
                    Phone = "11888888888"
                },
                SeatNumbers = new List<string> { "1C" }
            };

            var content = new StringContent(
            JsonSerializer.Serialize(createDto, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/reservations", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetById_ShouldReturnReservation_WhenExists()
        {
            var id = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD");

            var response = await _client.GetAsync($"/api/reservations/{id}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data.Id.Should().Be(id);
            content.Data.Status.Should().Be(ReservationStatus.Pending);
            content.Data.Seats.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenReservationDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/reservations/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAvailableSeats_ShouldReturnSeats_WhenTripExists()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");

            var response = await _client.GetAsync($"/api/reservations/trip/{tripId}/available");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<AvailableSeatsDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data.TripId.Should().Be(tripId);
            content.Data.TotalSeats.Should().Be(3);
            content.Data.AvailableSeats.Should().Be(2);
            content.Data.ReservedSeats.Should().Be(1);
            content.Data.SoldSeats.Should().Be(0);
        }

        [Fact]
        public async Task GetAvailableSeats_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            var tripId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/reservations/trip/{tripId}/available");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Cancel_ShouldCancelReservation_WhenExistsAndPending()
        {
            var id = Guid.Parse("DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD");

            var response = await _client.DeleteAsync($"/api/reservations/{id}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Reserva cancelada com sucesso");

            var getResponse = await _client.GetAsync($"/api/reservations/{id}");
            var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>();
            getResult.Data.Status.Should().Be(ReservationStatus.Cancelled);
        }

        [Fact]
        public async Task Cancel_ShouldReturnNotFound_WhenReservationDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await _client.DeleteAsync($"/api/reservations/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByTripId_ShouldReturnReservations_WhenTripExists()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");

            var response = await _client.GetAsync($"/api/reservations/trip/{tripId}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ReservationDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetByTripId_ShouldReturnEmpty_WhenNoReservations()
        {
            var tripId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/reservations/trip/{tripId}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ReservationDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByPassengerDocument_ShouldReturnReservations_WhenPassengerExists()
        {
            var document = "12345678901";

            var response = await _client.GetAsync($"/api/reservations/passenger/{document}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ReservationDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetByPassengerDocument_ShouldReturnEmpty_WhenPassengerNotFound()
        {
            var document = "99999999999";

            var response = await _client.GetAsync($"/api/reservations/passenger/{document}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<ReservationDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().BeEmpty();
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
            _host.Dispose();
            _client.Dispose();
        }
    }
}


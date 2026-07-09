using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    public class SeatsControllerIntegrationTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHost _host;
        private readonly string _databaseName;

        public SeatsControllerIntegrationTests()
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
    .AddApplicationPart(typeof(SeatsController).Assembly);
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
            _dbContext.Trips.RemoveRange(_dbContext.Trips);
            _dbContext.Buses.RemoveRange(_dbContext.Buses);
            _dbContext.Seats.RemoveRange(_dbContext.Seats);
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
                DepartureTime = now.AddDays(1).AddHours(8),
                ArrivalTime = now.AddDays(1).AddHours(11),
                BusId = bus.Id,
                Price = 120.00m,
                Status = TripStatus.Scheduled,
                IsActive = true,
                CreatedAt = now
            };

            _dbContext.Trips.Add(trip);
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
        }

        [Fact]
        public async Task GetById_ShouldReturnSeat_WhenExists()
        {
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var response = await _client.GetAsync($"/api/seats/{id}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data.Id.Should().Be(id);
            content.Data.Number.Should().Be("1A");
            content.Data.Status.Should().Be(SeatStatus.Available);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenSeatDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/seats/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByTripId_ShouldReturnAllSeats_WhenTripExists()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");

            var response = await _client.GetAsync($"/api/seats/trip/{tripId}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<SeatDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().HaveCount(3);
        }

        [Fact]
        public async Task GetByTripId_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            var tripId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/seats/trip/{tripId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_ShouldCreateNewSeat_WhenValid()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var newSeat = new CreateSeatDto
            {
                TripId = tripId,
                Number = "2A",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 2,
                Column = 1,
                PriceMultiplier = 1.10m
            };

            var content = new StringContent(
            JsonSerializer.Serialize(newSeat, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/seats", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Number.Should().Be("2A");
            result.Data.Status.Should().Be(SeatStatus.Available);
            result.Message.Should().Be("Assento criado com sucesso");
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenSeatNumberAlreadyExists()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var newSeat = new CreateSeatDto
            {
                TripId = tripId,
                Number = "1A",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 1,
                Column = 1,
                PriceMultiplier = 1.10m
            };

            var content = new StringContent(
            JsonSerializer.Serialize(newSeat, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/seats", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_ShouldUpdateExistingSeat_WhenValid()
        {
            var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var updateSeat = new UpdateSeatDto
            {
                Id = id,
                TripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
                Number = "1B",
                Type = SeatType.Window,
                Status = SeatStatus.Reserved,
                Row = 1,
                Column = 2,
                PriceMultiplier = 1.15m,
                IsActive = true
            };

            var content = new StringContent(
            JsonSerializer.Serialize(updateSeat, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PutAsync("/api/seats", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Type.Should().Be(SeatType.Window);
            result.Data.Status.Should().Be(SeatStatus.Reserved);
            result.Data.PriceMultiplier.Should().Be(1.15m);
            result.Message.Should().Be("Assento atualizado com sucesso");
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenSeatDoesNotExist()
        {
            var updateSeat = new UpdateSeatDto
            {
                Id = Guid.NewGuid(),
                TripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"),
                Number = "1B",
                Type = SeatType.Window,
                Status = SeatStatus.Available,
                Row = 1,
                Column = 2,
                PriceMultiplier = 1.10m,
                IsActive = true
            };

            var content = new StringContent(
            JsonSerializer.Serialize(updateSeat, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PutAsync("/api/seats", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_ShouldSoftDeleteSeat_WhenExists()
        {
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var response = await _client.DeleteAsync($"/api/seats/{id}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Assento removido com sucesso");
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenSeatDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await _client.DeleteAsync($"/api/seats/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateStatus_ShouldUpdateSeatStatus_WhenValid()
        {
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var updateStatus = new UpdateSeatStatusDto
            {
                Status = SeatStatus.Reserved,
                PassengerName = "João Silva",
                PassengerDocument = "12345678901"
            };

            var content = new StringContent(
            JsonSerializer.Serialize(updateStatus, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PatchAsync($"/api/seats/{id}/status", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<SeatDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Status.Should().Be(SeatStatus.Reserved);
            result.Data.PassengerName.Should().Be("João Silva");
            result.Data.PassengerDocument.Should().Be("12345678901");
            result.Message.Should().Be("Status do assento atualizado com sucesso");
        }

        [Fact]
        public async Task UpdateStatus_ShouldReturnNotFound_WhenSeatDoesNotExist()
        {
            var id = Guid.NewGuid();
            var updateStatus = new UpdateSeatStatusDto
            {
                Status = SeatStatus.Reserved
            };

            var content = new StringContent(
            JsonSerializer.Serialize(updateStatus, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PatchAsync($"/api/seats/{id}/status", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CheckNumber_ShouldReturnTrue_WhenNumberAvailable()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var number = "2A";

            var response = await _client.GetAsync($"/api/seats/check-number/{tripId}/{number}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Número disponível");
        }

        [Fact]
        public async Task CheckNumber_ShouldReturnFalse_WhenNumberAlreadyUsed()
        {
            var tripId = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var number = "1A";

            var response = await _client.GetAsync($"/api/seats/check-number/{tripId}/{number}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeFalse();
            result.Message.Should().Be("Número já utilizado");
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

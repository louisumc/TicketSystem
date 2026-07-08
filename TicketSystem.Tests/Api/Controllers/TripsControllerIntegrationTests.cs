using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using TicketSystem.Api.Controllers;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Application.Responses;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Infrastructure.Services;
using Xunit;

namespace TicketSystem.Tests.Api.Controllers
{
    public class TripsControllerIntegrationTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHost _host;
        private readonly string _databaseName;

        public TripsControllerIntegrationTests()
        {
            _databaseName = $"TicketSystemTestDb{Guid.NewGuid():N}";

            _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
    .UseTestServer()
    .ConfigureServices(services =>
    {
        var descriptor = services.SingleOrDefault(
d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
        if (descriptor != null)
            services.Remove(descriptor);

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

        services.AddControllers()
.AddApplicationPart(typeof(TripsController).Assembly);
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
            _dbContext.SaveChanges();

            var now = DateTime.UtcNow;

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

            var trips = new List<Trip>
{
new Trip
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
},
new Trip
{
Id = Guid.Parse("CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC"),
Origin = "São Paulo",
Destination = "Curitiba",
DepartureTime = now.AddDays(2).AddHours(10),
ArrivalTime = now.AddDays(2).AddHours(14),
BusId = bus.Id,
Price = 80.00m,
Status = TripStatus.Scheduled,
IsActive = true,
CreatedAt = now
}
};

            _dbContext.Trips.AddRange(trips);
            _dbContext.SaveChanges();

            Console.WriteLine($"{_dbContext.Trips.Count()} viagens inseridas no banco: {_databaseName}");
        }

        [Fact]
        public async Task GetAll_ShouldReturnAllTrips()
        {
            var response = await _client.GetAsync("/api/trips");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<TripResponseDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().HaveCount(2);
            content.Message.Should().Be("Viagens listadas com sucesso");
        }

        [Fact]
        public async Task GetById_ShouldReturnTrip_WhenExists()
        {
            var id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");

            var response = await _client.GetAsync($"/api/trips/{id}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponseDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data.Id.Should().Be(id);
            content.Data.Origin.Should().Be("São Paulo");
            content.Data.Destination.Should().Be("Rio de Janeiro");
            content.Data.Price.Should().Be(120.00m);
            content.Data.Status.Should().Be(TripStatus.Scheduled);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/trips/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByBusId_ShouldReturnTrips_WhenBusExists()
        {
            var busId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");

            var response = await _client.GetAsync($"/api/trips/bus/{busId}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<TripResponseDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().HaveCount(2);
            content.Data.Should().AllSatisfy(t => t.BusId.Should().Be(busId));
        }

        [Fact]
        public async Task GetByBusId_ShouldReturnNotFound_WhenBusDoesNotExist()
        {
            var busId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/trips/bus/{busId}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByStatus_ShouldReturnTrips_WithSpecificStatus()
        {
            var response = await _client.GetAsync("/api/trips/status/0");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<TripResponseDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().AllSatisfy(t => t.Status.Should().Be(TripStatus.Scheduled));
        }

        [Fact]
        public async Task Create_ShouldCreateNewTrip_WhenValid()
        {
            var busId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");
            var newTrip = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Belo Horizonte",
                DepartureTime = DateTime.UtcNow.AddHours(72),
                ArrivalTime = DateTime.UtcNow.AddHours(77),
                BusId = busId,
                Price = 150.00m,
                Status = TripStatus.Scheduled
            };

            var content = new StringContent(
            JsonSerializer.Serialize(newTrip, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/trips", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponseDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Origin.Should().Be("São Paulo");
            result.Data.Destination.Should().Be("Belo Horizonte");
            result.Data.Price.Should().Be(150.00m);
            result.Data.Status.Should().Be(TripStatus.Scheduled);
            result.Message.Should().Be("Viagem criada com sucesso");
            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenBusNotFound()
        {
            var newTrip = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Belo Horizonte",
                DepartureTime = DateTime.UtcNow.AddHours(72),
                ArrivalTime = DateTime.UtcNow.AddHours(77),
                BusId = Guid.NewGuid(),
                Price = 150.00m,
                Status = TripStatus.Scheduled
            };

            var content = new StringContent(
            JsonSerializer.Serialize(newTrip, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/trips", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenDepartureTimeIsPast()
        {
            var busId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");
            var newTrip = new CreateTripDto
            {
                Origin = "São Paulo",
                Destination = "Belo Horizonte",
                DepartureTime = DateTime.UtcNow.AddHours(-1),
                ArrivalTime = DateTime.UtcNow.AddHours(4),
                BusId = busId,
                Price = 150.00m,
                Status = TripStatus.Scheduled
            };

            var content = new StringContent(
            JsonSerializer.Serialize(newTrip, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/trips", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_ShouldUpdateExistingTrip_WhenValid()
        {
            var id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var busId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");
            var updateTrip = new UpdateTripDto
            {
                Id = id,
                Origin = "São Paulo Updated",
                Destination = "Rio de Janeiro Updated",
                DepartureTime = DateTime.UtcNow.AddHours(30),
                ArrivalTime = DateTime.UtcNow.AddHours(33),
                BusId = busId,
                Price = 180.00m,
                Status = TripStatus.Scheduled,
                IsActive = true
            };

            var content = new StringContent(
            JsonSerializer.Serialize(updateTrip, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PutAsync("/api/trips", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TripResponseDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(id);
            result.Data.Origin.Should().Be("São Paulo Updated");
            result.Data.Destination.Should().Be("Rio de Janeiro Updated");
            result.Data.Price.Should().Be(180.00m);
            result.Message.Should().Be("Viagem atualizada com sucesso");
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            var busId = Guid.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");
            var updateTrip = new UpdateTripDto
            {
                Id = Guid.NewGuid(),
                Origin = "Test",
                Destination = "Test",
                DepartureTime = DateTime.UtcNow.AddHours(30),
                ArrivalTime = DateTime.UtcNow.AddHours(33),
                BusId = busId,
                Price = 100.00m,
                Status = TripStatus.Scheduled,
                IsActive = true
            };

            var content = new StringContent(
            JsonSerializer.Serialize(updateTrip, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PutAsync("/api/trips", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_ShouldSoftDeleteTrip_WhenExists()
        {
            var id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");

            var response = await _client.DeleteAsync($"/api/trips/{id}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Viagem removida com sucesso");

            var getResponse = await _client.GetAsync($"/api/trips/{id}");
            var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<TripResponseDto>>();
            getResult.Data.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await _client.DeleteAsync($"/api/trips/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateStatus_ShouldUpdateTripStatus_WhenValid()
        {
            var id = Guid.Parse("BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB");
            var newStatus = TripStatus.InProgress;
            var content = new StringContent(
            JsonSerializer.Serialize(newStatus, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PatchAsync($"/api/trips/{id}/status", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Status da viagem atualizado com sucesso");

            var getResponse = await _client.GetAsync($"/api/trips/{id}");
            var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<TripResponseDto>>();
            getResult.Data.Status.Should().Be(TripStatus.InProgress);
        }

        [Fact]
        public async Task UpdateStatus_ShouldReturnNotFound_WhenTripDoesNotExist()
        {
            var id = Guid.NewGuid();
            var newStatus = TripStatus.InProgress;
            var content = new StringContent(
            JsonSerializer.Serialize(newStatus, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PatchAsync($"/api/trips/{id}/status", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
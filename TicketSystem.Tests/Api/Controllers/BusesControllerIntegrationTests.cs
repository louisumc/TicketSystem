using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Application.Responses;
using TicketSystem.Domain.Entities;
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
    public class BusesControllerIntegrationTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ApplicationDbContext _dbContext;
        private readonly IHost _host;
        private readonly string _databaseName;

        public BusesControllerIntegrationTests()
        {
            // CADA TESTE USA UM BANCO DIFERENTE
            _databaseName = $"TicketSystemTestDb{Guid.NewGuid():N}";

            _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
    .UseTestServer()
    .ConfigureServices(services =>
            {
                // REMOVE QUALQUER CONFIGURAÇÃO ANTERIOR
                var descriptor = services.SingleOrDefault(
    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // ADICIONA COM NOME ÚNICO
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
    .AddApplicationPart(typeof(BusesController).Assembly);
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

            // GARANTE QUE O BANCO ESTÁ LIMPO E CRIADO
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            // LIMPA TUDO
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();
            //_dbContext.Trips.RemoveRange(_dbContext.Trips);
            //_dbContext.Buses.RemoveRange(_dbContext.Buses);
            //_dbContext.SaveChanges();

            var now = DateTime.UtcNow;

            var buses = new List<Bus>
{
new Bus
{
Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
Plate = "ABC1234",
Model = "Mercedes Benz O500",
Company = "Viação Expresso",
Capacity = 45,
IsActive = true,
CreatedAt = now
},
new Bus
{
Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
Plate = "DEF5678",
Model = "Scania K400",
Company = "Viação Rápida",
Capacity = 50,
IsActive = true,
CreatedAt = now
},
new Bus
{
Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
Plate = "GHI9012",
Model = "Volvo 9800",
Company = "Viação Conforto",
Capacity = 55,
IsActive = false,
CreatedAt = now
}
};

            _dbContext.Buses.AddRange(buses);
            _dbContext.SaveChanges();

            // VERIFICA
            var count = _dbContext.Buses.Count();
            Console.WriteLine($"{count} ônibus inseridos no banco: {_databaseName}");
        }

        [Fact]
        public async Task GetAll_ShouldReturnAllBuses()
        {
            var response = await _client.GetAsync("/api/buses");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<BusResponseDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().HaveCount(3);
            content.Message.Should().Be("Ônibus listados com sucesso");
        }

        [Fact]
        public async Task GetActive_ShouldReturnOnlyActiveBuses()
        {
            var response = await _client.GetAsync("/api/buses/active");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<IEnumerable<BusResponseDto>>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().HaveCount(2);
            content.Data.Should().AllSatisfy(b => b.IsActive.Should().BeTrue());
        }

        [Fact]
        public async Task GetById_ShouldReturnBus_WhenExists()
        {
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var response = await _client.GetAsync($"/api/buses/{id}");
            var content = await response.Content.ReadFromJsonAsync<ApiResponse<BusResponseDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull();
            content.Success.Should().BeTrue();
            content.Data.Should().NotBeNull();
            content.Data.Id.Should().Be(id);
            content.Data.Plate.Should().Be("ABC1234");
            content.Data.Model.Should().Be("Mercedes Benz O500");
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenBusDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/buses/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_ShouldCreateNewBus_WhenValid()
        {
            var newBus = new CreateBusDto
            {
                Plate = "XYZ9999",
                Model = "New Bus Model",
                Company = "New Company",
                Capacity = 40
            };

            var content = new StringContent(
            JsonSerializer.Serialize(newBus, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/buses", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<BusResponseDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Plate.Should().Be("XYZ9999");
            result.Data.Model.Should().Be("New Bus Model");
            result.Data.Company.Should().Be("New Company");
            result.Data.Capacity.Should().Be(40);
            result.Data.IsActive.Should().BeTrue();
            result.Message.Should().Be("Ônibus criado com sucesso");
            response.Headers.Location.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenPlateAlreadyExists()
        {
            var existingBus = new CreateBusDto
            {
                Plate = "ABC1234",
                Model = "Duplicate Bus",
                Company = "Duplicate Company",
                Capacity = 30
            };

            var content = new StringContent(
            JsonSerializer.Serialize(existingBus, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PostAsync("/api/buses", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_ShouldUpdateExistingBus_WhenValid()
        {
            var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var updateBus = new UpdateBusDto
            {
                Id = id,
                Plate = "DEF9999",
                Model = "Updated Model",
                Company = "Updated Company",
                Capacity = 60,
                IsActive = true
            };

            var content = new StringContent(
            JsonSerializer.Serialize(updateBus, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PutAsync("/api/buses", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<BusResponseDto>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(id);
            result.Data.Plate.Should().Be("DEF9999");
            result.Data.Model.Should().Be("Updated Model");
            result.Data.Company.Should().Be("Updated Company");
            result.Data.Capacity.Should().Be(60);
            result.Message.Should().Be("Ônibus atualizado com sucesso");
        }

        [Fact]
        public async Task Update_ShouldReturnNotFound_WhenBusDoesNotExist()
        {
            var updateBus = new UpdateBusDto
            {
                Id = Guid.NewGuid(),
                Plate = "XYZ9999",
                Model = "Test",
                Company = "Test",
                Capacity = 30,
                IsActive = true
            };

            var content = new StringContent(
            JsonSerializer.Serialize(updateBus, _jsonOptions),
            Encoding.UTF8,
            "application/json");

            var response = await _client.PutAsync("/api/buses", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_ShouldSoftDeleteBus_WhenExists()
        {
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

            var response = await _client.DeleteAsync($"/api/buses/{id}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Ônibus removido com sucesso");

            var getResponse = await _client.GetAsync($"/api/buses/{id}");
            var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<BusResponseDto>>();
            getResult.Data.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task Delete_ShouldReturnNotFound_WhenBusDoesNotExist()
        {
            var id = Guid.NewGuid();

            var response = await _client.DeleteAsync($"/api/buses/{id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CheckPlate_ShouldReturnTrue_WhenPlateExists()
        {
            var plate = "ABC1234";

            var response = await _client.GetAsync($"/api/buses/check-plate/{plate}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
            result.Message.Should().Be("Placa já existe");
        }

        [Fact]
        public async Task CheckPlate_ShouldReturnFalse_WhenPlateNotExists()
        {
            var plate = "NONEXIST";

            var response = await _client.GetAsync($"/api/buses/check-plate/{plate}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().BeFalse();
            result.Message.Should().Be("Placa disponível");
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
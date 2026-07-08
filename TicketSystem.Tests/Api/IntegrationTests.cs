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
using TicketSystem.Api.Controllers;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Infrastructure.Services;
using Xunit;

namespace TicketSystem.Tests.Api
{
    public class IntegrationTests : IDisposable
    {
        private readonly HttpClient _client;
        private readonly IHost _host;

        public IntegrationTests()
        {
            _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
    .UseTestServer()
    .ConfigureServices(services =>
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TicketSystemTestDb_{Guid.NewGuid():N}");
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
        }

        [Fact]
        public async Task Api_ShouldReturnOk_WhenBusesEndpointIsCalled()
        {
            var response = await _client.GetAsync("/api/buses");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Api_ShouldReturnOk_WhenTripsEndpointIsCalled()
        {
            var response = await _client.GetAsync("/api/trips");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        public void Dispose()
        {
            _host.Dispose();
            _client.Dispose();
        }
    }
}
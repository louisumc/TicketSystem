using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Scrutor;
using Serilog;
using StackExchange.Redis;
using TicketSystem.Api.Middleware;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Application.Validators.Bus;
using TicketSystem.Application.Validators.Passenger;
using TicketSystem.Application.Validators.Reservation;
using TicketSystem.Application.Validators.Seat;
using TicketSystem.Application.Validators.Trip;
using TicketSystem.Infrastructure.Cache;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Infrastructure.Health;
using TicketSystem.Infrastructure.Locks;
using TicketSystem.Infrastructure.Messaging;
using TicketSystem.Infrastructure.Messaging.Consumers;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Infrastructure.Services;
using TicketSystem.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
.ReadFrom.Configuration(builder.Configuration)
.Enrich.FromLogContext()
.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
.WriteTo.File("logs/ticketsystem-.txt",
rollingInterval: RollingInterval.Day,
outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
.CreateLogger();

builder.Host.UseSerilog();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(
builder.Configuration.GetConnectionString("DefaultConnection"),
sqlOptions => sqlOptions.EnableRetryOnFailure(
maxRetryCount: 5,
maxRetryDelay: TimeSpan.FromSeconds(10),
errorNumbersToAdd: null
)
));

// Configure Redis
var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled");
if (redisEnabled)
{
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
    builder.Services.AddScoped<ICacheService, RedisCacheService>();
    builder.Services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
}
else
{
    builder.Services.AddScoped<ICacheService, NullCacheService>();
    builder.Services.AddScoped<IDistributedLockService, NullDistributedLockService>();
}

// Configure RabbitMQ
var rabbitMQEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", false);
if (rabbitMQEnabled)
{
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration.GetValue<string>("RabbitMQ:HostName", "localhost"),
        Port = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672),
        UserName = builder.Configuration.GetValue<string>("RabbitMQ:UserName", "guest"),
        Password = builder.Configuration.GetValue<string>("RabbitMQ:Password", "guest"),
        VirtualHost = builder.Configuration.GetValue<string>("RabbitMQ:VirtualHost", "/"),
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
    };

    var connection = factory.CreateConnection();
    builder.Services.AddSingleton<IConnection>(connection);
    builder.Services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();
}
else
{
    builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();
}

// Health Checks
builder.Services.AddHealthChecks()
.AddCheck<RedisHealthCheck>("redis")
.AddDbContextCheck<ApplicationDbContext>("database");

// Configure AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Configure FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateBusDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTripDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateSeatDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateReservationDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateBusDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateTripDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateSeatDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateSeatStatusDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ConfirmReservationDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreatePassengerDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdatePassengerDtoValidator>();

// Configure Dependency Injection
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IBusService, BusService>();
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Registra TripService e CacheTripService
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.Decorate<ITripService, CacheTripService>();

// Registra ReservationService e CacheReservationService
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.Decorate<IReservationService, CacheReservationService>();

// Registra consumers como hosted services
if (rabbitMQEnabled)
{
    builder.Services.AddHostedService<ReservationCreatedConsumer>();
    builder.Services.AddHostedService<ReservationConfirmedConsumer>();
    builder.Services.AddHostedService<PaymentFailedConsumer>();
    builder.Services.AddHostedService<TicketGeneratedConsumer>();
}

// Workers
builder.Services.AddHostedService<ReservationExpirationWorker>();
builder.Services.AddHostedService<TicketGenerationWorker>();
builder.Services.AddHostedService<EmailNotificationWorker>();
builder.Services.AddHostedService<PaymentRetryWorker>();

// Email Service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddLogging();

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<TicketSystem.Application.Validators.Bus.CreateBusDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<TicketSystem.Application.Validators.Bus.UpdateBusDtoValidator>();

// Configure Authorization
builder.Services.AddAuthorization();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Ticket System API",
        Version = "v1",
        Description = "API para gerenciamento de vendas de passagens de onibus",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Ticket System Team",
            Email = "support@ticketsystem.com"
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket System API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Health Check endpoint
app.MapHealthChecks("/health");

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (dbContext.Database.GetPendingMigrations().Any())
    {
        dbContext.Database.Migrate();
    }
}

app.Run();


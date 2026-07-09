// FILENAME: TicketSystem.Api/Program.cs
﻿using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Api.Middleware;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Mappings;
using TicketSystem.Application.Validators.Bus;
using TicketSystem.Application.Validators.Seat;
using TicketSystem.Application.Validators.Trip;
using TicketSystem.Infrastructure.Data;
using TicketSystem.Infrastructure.Repositories;
using TicketSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Configure FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateBusDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateTripDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateSeatDtoValidator>();


// Configure Dependency Injection
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IBusService, BusService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ISeatService, SeatService>();

// Add Controllers
builder.Services.AddControllers();

// These methods extend IServiceCollection
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<TicketSystem.Application.Validators.Bus.CreateBusDtoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<TicketSystem.Application.Validators.Bus.UpdateBusDtoValidator>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
{
Title = "Ticket System API",
Version = "v1",
Description = "API para gerenciamento de vendas de passagens de ônibus",
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

// Add global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
if (dbContext.Database.GetPendingMigrations().Any())
{
dbContext.Database.Migrate();
}
}

app.Run();


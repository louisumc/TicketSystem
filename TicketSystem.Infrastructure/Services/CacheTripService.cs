using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Cache;

namespace TicketSystem.Infrastructure.Services
{
public class CacheTripService : ITripService
{
private readonly ITripService _innerService;
private readonly ICacheService _cacheService;
private readonly ILogger<CacheTripService> _logger;
private readonly IConfiguration _configuration;

public CacheTripService(
ITripService innerService,
ICacheService cacheService,
ILogger<CacheTripService> logger,
IConfiguration configuration)
{
_innerService = innerService;
_cacheService = cacheService;
_logger = logger;
_configuration = configuration;
}

private TimeSpan GetExpiration(string key)
{
var seconds = _configuration.GetValue<int>($"Redis:CacheExpiration:{key}");
return TimeSpan.FromSeconds(seconds);
}

public async Task<TripResponseDto> GetTripResponseByIdAsync(Guid id)
{
var key = CacheKeys.GetTripDetailsKey(id);
var expiration = GetExpiration("TripDetails");

return await _cacheService.GetOrSetAsync(key, async () =>
{
_logger.LogDebug("Cache miss para TripDetails: {Id}", id);
return await _innerService.GetTripResponseByIdAsync(id);
}, expiration);
}

public async Task<TripDetailsDto> GetTripDetailsByIdAsync(Guid id)
{
var key = CacheKeys.GetTripDetailsKey(id);
var expiration = GetExpiration("TripDetails");

return await _cacheService.GetOrSetAsync(key, async () =>
{
_logger.LogDebug("Cache miss para TripDetails: {Id}", id);
return await _innerService.GetTripDetailsByIdAsync(id);
}, expiration);
}

public async Task<IEnumerable<TripResponseDto>> GetAllTripResponsesAsync()
{
var key = CacheKeys.TripsListKey;
var expiration = GetExpiration("TripsList");

return await _cacheService.GetOrSetAsync(key, async () =>
{
_logger.LogDebug("Cache miss para TripsList");
return await _innerService.GetAllTripResponsesAsync();
}, expiration);
}

public async Task<IEnumerable<TripResponseDto>> GetByBusIdAsync(Guid busId)
{
var key = CacheKeys.GetTripByBusKey(busId);
var expiration = GetExpiration("TripsList");

return await _cacheService.GetOrSetAsync(key, async () =>
{
_logger.LogDebug("Cache miss para TripByBus: {BusId}", busId);
return await _innerService.GetByBusIdAsync(busId);
}, expiration);
}

public async Task<IEnumerable<TripResponseDto>> GetByStatusAsync(TripStatus status)
{
var key = CacheKeys.GetTripByStatusKey((int)status);
var expiration = GetExpiration("TripsList");

return await _cacheService.GetOrSetAsync(key, async () =>
{
_logger.LogDebug("Cache miss para TripByStatus: {Status}", status);
return await _innerService.GetByStatusAsync(status);
}, expiration);
}

public async Task<IEnumerable<TripResponseDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
{
var key = CacheKeys.GetTripByDateRangeKey(startDate, endDate);
var expiration = GetExpiration("TripsList");

return await _cacheService.GetOrSetAsync(key, async () =>
{
_logger.LogDebug("Cache miss para TripByDateRange: {Start} - {End}", startDate, endDate);
return await _innerService.GetByDateRangeAsync(startDate, endDate);
}, expiration);
}

public async Task<TripResponseDto> CreateTripAsync(CreateTripDto createTripDto)
{
var result = await _innerService.CreateTripAsync(createTripDto);
await InvalidateTripCacheAsync();
return result;
}

public async Task<TripDetailsDto> CreateTripWithSeatsAsync(CreateTripDto createTripDto)
{
var result = await _innerService.CreateTripWithSeatsAsync(createTripDto);
await InvalidateTripCacheAsync();
return result;
}

public async Task<TripResponseDto> UpdateTripAsync(UpdateTripDto updateTripDto)
{
var result = await _innerService.UpdateTripAsync(updateTripDto);
await InvalidateTripCacheAsync();
return result;
}

public async Task DeleteTripAsync(Guid id)
{
await _innerService.DeleteTripAsync(id);
await InvalidateTripCacheAsync();
}

public async Task UpdateStatusAsync(Guid id, TripStatus newStatus)
{
await _innerService.UpdateStatusAsync(id, newStatus);
await InvalidateTripCacheAsync();
}

public async Task<Trip?> GetByIdAsync(Guid id)
{
return await _innerService.GetByIdAsync(id);
}

public async Task<IEnumerable<Trip>> GetAllAsync()
{
return await _innerService.GetAllAsync();
}

public async Task<IEnumerable<Trip>> FindAsync(Expression<Func<Trip, bool>> predicate)
{
return await _innerService.FindAsync(predicate);
}

public async Task<Trip> AddAsync(Trip entity)
{
return await _innerService.AddAsync(entity);
}

public async Task UpdateAsync(Trip entity)
{
await _innerService.UpdateAsync(entity);
}

public async Task DeleteAsync(Trip entity)
{
await _innerService.DeleteAsync(entity);
}

public async Task<bool> ExistsAsync(Expression<Func<Trip, bool>> predicate)
{
return await _innerService.ExistsAsync(predicate);
}

public async Task<int> CountAsync(Expression<Func<Trip, bool>>? predicate = null)
{
return await _innerService.CountAsync(predicate);
}

private async Task InvalidateTripCacheAsync()
{
try
{
_logger.LogDebug("Invalidando cache de viagens...");
foreach (var pattern in CacheKeys.AllTripPatterns)
{
await _cacheService.RemoveByPatternAsync(pattern);
}
}
catch (Exception ex)
{
_logger.LogError(ex, "Erro ao invalidar cache de viagens");
}
}
}
}
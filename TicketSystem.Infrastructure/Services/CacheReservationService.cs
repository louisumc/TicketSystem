using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;
using TicketSystem.Infrastructure.Cache;

namespace TicketSystem.Infrastructure.Services
{
    public class CacheReservationService : IReservationService
    {
        private readonly IReservationService _innerService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheReservationService> _logger;
        private readonly IConfiguration _configuration;

        public CacheReservationService(
        IReservationService innerService,
        ICacheService cacheService,
        ILogger<CacheReservationService> logger,
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

        public async Task<ReservationDto> GetReservationByIdAsync(Guid id)
        {
            var key = CacheKeys.GetReservationKey(id);
            var expiration = GetExpiration("Reservation");

            return await _cacheService.GetOrSetAsync(key, async () =>
            {
                _logger.LogDebug("Cache miss para Reservation: {Id}", id);
                return await _innerService.GetReservationByIdAsync(id);
            }, expiration);
        }

        public async Task<ReservationDto> CreateReservationAsync(CreateReservationDto createDto)
        {
            var result = await _innerService.CreateReservationAsync(createDto);
            await InvalidateReservationCacheAsync(result.Id);
            return result;
        }

        public async Task<ReservationDto> ConfirmReservationAsync(ConfirmReservationDto confirmDto)
        {
            var result = await _innerService.ConfirmReservationAsync(confirmDto);
            await InvalidateReservationCacheAsync(result.Id);
            return result;
        }

        public async Task CancelReservationAsync(Guid reservationId)
        {
            await _innerService.CancelReservationAsync(reservationId);
            await InvalidateReservationCacheAsync(reservationId);
        }

        public async Task<AvailableSeatsDto> GetAvailableSeatsAsync(Guid tripId)
        {
            var key = CacheKeys.GetAvailableSeatsKey(tripId);
            var expiration = GetExpiration("AvailableSeats");

            return await _cacheService.GetOrSetAsync(key, async () =>
            {
                _logger.LogDebug("Cache miss para AvailableSeats: {TripId}", tripId);
                return await _innerService.GetAvailableSeatsAsync(tripId);
            }, expiration);
        }

        public async Task<IEnumerable<ReservationDto>> GetReservationsByTripIdAsync(Guid tripId)
        {
            return await _innerService.GetReservationsByTripIdAsync(tripId);
        }

        public async Task<IEnumerable<ReservationDto>> GetReservationsByPassengerDocumentAsync(string document)
        {
            return await _innerService.GetReservationsByPassengerDocumentAsync(document);
        }

        public async Task ExpirePendingReservationsAsync()
        {
            await _innerService.ExpirePendingReservationsAsync();
        }

        public async Task<Reservation?> GetByIdAsync(Guid id)
        {
            return await _innerService.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Reservation>> GetAllAsync()
        {
            return await _innerService.GetAllAsync();
        }

        public async Task<IEnumerable<Reservation>> FindAsync(Expression<Func<Reservation, bool>> predicate)
        {
            return await _innerService.FindAsync(predicate);
        }

        public async Task<Reservation> AddAsync(Reservation entity)
        {
            return await _innerService.AddAsync(entity);
        }

        public async Task UpdateAsync(Reservation entity)
        {
            await _innerService.UpdateAsync(entity);
        }

        public async Task DeleteAsync(Reservation entity)
        {
            await _innerService.DeleteAsync(entity);
        }

        public async Task<bool> ExistsAsync(Expression<Func<Reservation, bool>> predicate)
        {
            return await _innerService.ExistsAsync(predicate);
        }

        public async Task<int> CountAsync(Expression<Func<Reservation, bool>>? predicate = null)
        {
            return await _innerService.CountAsync(predicate);
        }

        private async Task InvalidateReservationCacheAsync(Guid reservationId)
        {
            try
            {
                _logger.LogDebug("Invalidando cache de reserva: {ReservationId}", reservationId);
                await _cacheService.RemoveAsync(CacheKeys.GetReservationKey(reservationId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao invalidar cache de reserva");
            }
        }
    }
}
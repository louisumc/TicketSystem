using Microsoft.Extensions.Logging;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Cache
{
    public class NullCacheService : ICacheService
    {
        private readonly ILogger<NullCacheService> _logger;

        public NullCacheService(ILogger<NullCacheService> logger)
        {
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            return Task.FromResult(default(T));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            _logger.LogDebug("Cache desabilitado. Set ignorado para chave: {Key}", key);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            return await factory();
        }
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Cache
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly bool _isEnabled;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, IConfiguration configuration, ILogger<RedisCacheService> logger)
        {
            _database = connectionMultiplexer.GetDatabase();
            _logger = logger;
            _isEnabled = configuration.GetValue<bool>("Redis:Enabled");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            if (!_isEnabled)
            {
                _logger.LogDebug("Redis desabilitado. Pulando GET para chave: {Key}", key);
                return default;
            }

            try
            {
                _logger.LogDebug("Redis GET - Verificando chave: {Key}", key);
                var value = await _database.StringGetAsync(key);

                if (value.HasValue)
                {
                    _logger.LogInformation("CACHE HIT - Chave: {Key}", key);
                    return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
                }

                _logger.LogInformation("CACHE MISS - Chave: {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter cache para chave: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            if (!_isEnabled)
            {
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);
                var ttl = expiration.HasValue ? expiration.Value.ToString() : "sem expiracao";
                _logger.LogInformation("CACHE SET - Chave: {Key}, TTL: {TTL}, Tamanho: {Size} bytes",
                key, ttl, json.Length);

                if (expiration.HasValue)
                {
                    await _database.StringSetAsync(key, json, expiration.Value);
                }
                else
                {
                    await _database.StringSetAsync(key, json);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar cache para chave: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                return;
            }

            try
            {
                _logger.LogInformation("CACHE REMOVE - Chave: {Key}", key);
                await _database.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover cache para chave: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                return;
            }

            try
            {
                _logger.LogInformation("CACHE REMOVE BY PATTERN - Padrao: {Pattern}", pattern);
                var endpoints = _database.Multiplexer.GetEndPoints();
                if (endpoints.Length == 0)
                {
                    return;
                }

                var server = _database.Multiplexer.GetServer(endpoints.First());
                var keys = server.Keys(pattern: pattern).ToList();

                _logger.LogDebug("Encontradas {Count} chaves para remover com padrao: {Pattern}", keys.Count, pattern);

                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover cache por padrao: {Pattern}", pattern);
            }
        }

        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                return false;
            }

            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar existencia de cache para chave: {Key}", key);
                return false;
            }
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
        {
            if (!_isEnabled)
            {
                return await factory();
            }

            try
            {
                var cached = await GetAsync<T>(key, cancellationToken);
                if (cached != null)
                {
                    return cached;
                }

                var result = await factory();
                if (result != null)
                {
                    await SetAsync(key, result, expiration, cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter ou definir cache para chave: {Key}", key);
                return await factory();
            }
        }
    }
}
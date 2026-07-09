using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace TicketSystem.Infrastructure.Health
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(IConnectionMultiplexer redis, ILogger<RedisHealthCheck> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var isConnected = _redis.IsConnected;
                var database = _redis.GetDatabase();
                var ping = await database.PingAsync();

                var data = new Dictionary<string, object>
{
{ "IsConnected", isConnected },
{ "Ping", ping.TotalMilliseconds + "ms" },
{ "Endpoints", string.Join(", ", _redis.GetEndPoints().Select(e => e.ToString())) }
};

                if (isConnected && ping.TotalMilliseconds < 100)
                {
                    return HealthCheckResult.Healthy("Redis conectado e respondendo rapidamente", data);
                }

                if (isConnected)
                {
                    return HealthCheckResult.Degraded("Redis conectado mas com latencia elevada", null, data);
                }

                return HealthCheckResult.Unhealthy("Redis nao conectado", null, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no health check do Redis");
                return HealthCheckResult.Unhealthy("Erro ao verificar Redis", ex);
            }
        }
    }
}
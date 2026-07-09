using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Responses;

namespace TicketSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IConnectionMultiplexer? _redis;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
        IConnectionMultiplexer? redis,
        IConfiguration configuration,
        ILogger<DiagnosticsController> logger)
        {
            _redis = redis;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            var isRedisEnabled = _configuration.GetValue<bool>("Redis:Enabled");
            var diagnostics = new
            {
                RedisEnabled = isRedisEnabled,
                RedisConnected = _redis?.IsConnected ?? false,
                RedisConfiguration = _redis?.Configuration ?? "Nao configurado",
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Diagnostico: {@Diagnostics}", diagnostics);

            return Ok(new ApiResponse<object>(diagnostics, "Diagnostico realizado com sucesso"));
        }

        [HttpGet("cache/{key}")]
        public async Task<IActionResult> GetCacheKey([FromServices] ICacheService cacheService, string key)
        {
            try
            {
                var exists = await cacheService.ExistsAsync(key);
                object? value = null;

                if (exists)
                {
                    value = await cacheService.GetAsync<object>(key);
                }

                TimeSpan? ttl = null;
                if (_redis != null && _redis.IsConnected)
                {
                    ttl = await _redis.GetDatabase().KeyTimeToLiveAsync(key);
                }

                return Ok(new
                {
                    Key = key,
                    Exists = exists,
                    Value = value,
                    TTL = ttl
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("cache/{key}")]
        public async Task<IActionResult> ClearCacheKey([FromServices] ICacheService cacheService, string key)
        {
            await cacheService.RemoveAsync(key);
            return Ok(new { Message = "Cache removido", Key = key });
        }

        [HttpDelete("cache")]
        public async Task<IActionResult> ClearAllCache([FromServices] ICacheService cacheService)
        {
            if (_redis?.IsConnected == true)
            {
                var endpoints = _redis.GetEndPoints();
                var server = _redis.GetServer(endpoints.First());
                await server.FlushDatabaseAsync();
                return Ok(new { Message = "Todos os caches removidos" });
            }

            return BadRequest(new { Message = "Redis nao conectado" });
        }
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Locks
{
public class RedisDistributedLockService : IDistributedLockService
{
private readonly IDatabase _database;
private readonly ILogger<RedisDistributedLockService> _logger;
private readonly bool _isEnabled;
private readonly string _instanceId;
private readonly int _lockTimeoutSeconds;
private readonly int _lockRetryCount;
private readonly int _lockRetryBackoffMs;

public RedisDistributedLockService(IConnectionMultiplexer connectionMultiplexer, IConfiguration configuration, ILogger<RedisDistributedLockService> logger)
{
_database = connectionMultiplexer.GetDatabase();
_logger = logger;
_isEnabled = configuration.GetValue<bool>("Redis:Enabled");
_instanceId = Guid.NewGuid().ToString();
_lockTimeoutSeconds = configuration.GetValue<int>("Redis:LockTimeoutSeconds", 10);
_lockRetryCount = configuration.GetValue<int>("Redis:LockRetryCount", 5);
_lockRetryBackoffMs = configuration.GetValue<int>("Redis:LockRetryBackoffMs", 100);
}

public async Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
{
if (!_isEnabled)
{
_logger.LogWarning("Redis desabilitado. Lock nao adquirido para: {ResourceKey}", resourceKey);
return new NullDisposableLock();
}

var lockKey = GetLockKey(resourceKey);
var lockValue = $"{_instanceId}:{Guid.NewGuid():N}";
var expiry = timeout ?? TimeSpan.FromSeconds(_lockTimeoutSeconds);

_logger.LogDebug("Tentando adquirir lock para: {ResourceKey}", resourceKey);

var retryCount = 0;
var maxRetries = _lockRetryCount;
var backoff = TimeSpan.FromMilliseconds(_lockRetryBackoffMs);

while (retryCount < maxRetries)
{
cancellationToken.ThrowIfCancellationRequested();

var acquired = await _database.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);

if (acquired)
{
_logger.LogInformation("Lock adquirido para: {ResourceKey}", resourceKey);
return new RedisDistributedLock(_database, lockKey, lockValue);
}

retryCount++;
var delay = backoff * retryCount;
_logger.LogDebug("Lock nao adquirido para: {ResourceKey}. Tentativa {Attempt}/{MaxRetries}. Aguardando {Delay}ms",
resourceKey, retryCount, maxRetries, delay.TotalMilliseconds);

await Task.Delay(delay, cancellationToken);
}

_logger.LogWarning("Lock nao adquirido apos {MaxRetries} tentativas para: {ResourceKey}", maxRetries, resourceKey);
throw new TimeoutException($"Nao foi possivel adquirir lock para {resourceKey} apos {maxRetries} tentativas");
}

public async Task<bool> TryAcquireLockAsync(string resourceKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
{
if (!_isEnabled)
{
return false;
}

try
{
using var lockObj = await AcquireLockAsync(resourceKey, timeout, cancellationToken);
return lockObj != null;
}
catch
{
return false;
}
}

public async Task ReleaseLockAsync(string resourceKey, CancellationToken cancellationToken = default)
{
if (!_isEnabled)
{
return;
}

var lockKey = GetLockKey(resourceKey);
var script = @"
if redis.call('exists', KEYS[1]) == 1 then
return redis.call('del', KEYS[1])
else
return 0
end";

await _database.ScriptEvaluateAsync(
script,
new RedisKey[] { lockKey },
new RedisValue[] { });

_logger.LogDebug("Lock liberado para: {ResourceKey}", resourceKey);
}

public async Task<bool> IsLockedAsync(string resourceKey, CancellationToken cancellationToken = default)
{
if (!_isEnabled)
{
return false;
}

var lockKey = GetLockKey(resourceKey);
return await _database.KeyExistsAsync(lockKey);
}

private static string GetLockKey(string resourceKey)
{
return $"lock:{resourceKey}";
}
}

public class NullDisposableLock : IDisposable
{
public void Dispose() { }
}
}
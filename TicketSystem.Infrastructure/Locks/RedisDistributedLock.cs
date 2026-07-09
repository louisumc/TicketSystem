using StackExchange.Redis;

namespace TicketSystem.Infrastructure.Locks
{
    public class RedisDistributedLock : IDisposable
    {
        private readonly IDatabase _database;
        private readonly string _lockKey;
        private readonly string _lockValue;
        private bool _disposed;

        public RedisDistributedLock(IDatabase database, string lockKey, string lockValue)
        {
            _database = database;
            _lockKey = lockKey;
            _lockValue = lockValue;
        }

        public async Task<bool> AcquireAsync(TimeSpan? timeout = null)
        {
            var expiry = timeout ?? TimeSpan.FromSeconds(30);
            return await _database.StringSetAsync(_lockKey, _lockValue, expiry, When.NotExists);
        }

        public async Task<bool> ReleaseAsync()
        {
            var script = @"
if redis.call('get', KEYS[1]) == ARGV[1] then
return redis.call('del', KEYS[1])
else
return 0
end";

            var result = await _database.ScriptEvaluateAsync(
            script,
            new RedisKey[] { _lockKey },
            new RedisValue[] { _lockValue });

            return (int)result == 1;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ReleaseAsync().GetAwaiter().GetResult();
                _disposed = true;
            }
        }
    }
}
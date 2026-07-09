using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Locks
{
    public class NullDistributedLockService : IDistributedLockService
    {
        public Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDisposable>(new NullDisposableLock());
        }

        public Task<bool> TryAcquireLockAsync(string resourceKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task ReleaseLockAsync(string resourceKey, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> IsLockedAsync(string resourceKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }
}
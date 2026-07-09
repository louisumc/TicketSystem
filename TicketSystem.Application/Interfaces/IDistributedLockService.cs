namespace TicketSystem.Application.Interfaces
{
    public interface IDistributedLockService
    {
        Task<IDisposable> AcquireLockAsync(string resourceKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
        Task<bool> TryAcquireLockAsync(string resourceKey, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
        Task ReleaseLockAsync(string resourceKey, CancellationToken cancellationToken = default);
        Task<bool> IsLockedAsync(string resourceKey, CancellationToken cancellationToken = default);
    }
}
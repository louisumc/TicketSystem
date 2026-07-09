namespace TicketSystem.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
        Task PublishWithRetryAsync<T>(T @event, int maxRetries = 3, CancellationToken cancellationToken = default) where T : class;
    }
}
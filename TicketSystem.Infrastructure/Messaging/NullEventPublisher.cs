using Microsoft.Extensions.Logging;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Messaging
{
    public class NullEventPublisher : IEventPublisher
    {
        private readonly ILogger<NullEventPublisher> _logger;

        public NullEventPublisher(ILogger<NullEventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        {
            _logger.LogDebug("EventPublisher desabilitado. Evento ignorado: {EventType}", typeof(T).Name);
            return Task.CompletedTask;
        }

        public Task PublishWithRetryAsync<T>(T @event, int maxRetries = 3, CancellationToken cancellationToken = default) where T : class
        {
            _logger.LogDebug("EventPublisher desabilitado. Evento ignorado: {EventType}", typeof(T).Name);
            return Task.CompletedTask;
        }
    }
}
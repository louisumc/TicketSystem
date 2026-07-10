using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using TicketSystem.Application.Events;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Messaging
{
    public class RabbitMQEventPublisher : IEventPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMQEventPublisher> _logger;
        private readonly bool _isEnabled;
        private readonly int _retryCount;
        private readonly int _retryDelayMs;
        private readonly JsonSerializerOptions _jsonOptions;

        public RabbitMQEventPublisher(IConfiguration configuration, ILogger<RabbitMQEventPublisher> logger)
        {
            _logger = logger;
            _isEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", false);
            _retryCount = configuration.GetValue<int>("RabbitMQ:RetryCount", 3);
            _retryDelayMs = configuration.GetValue<int>("RabbitMQ:RetryDelayMs", 1000);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            if (!_isEnabled)
            {
                _logger.LogInformation("RabbitMQ desabilitado. Eventos serao ignorados.");
                return;
            }

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = configuration.GetValue<string>("RabbitMQ:HostName", "localhost"),
                    Port = configuration.GetValue<int>("RabbitMQ:Port", 5672),
                    UserName = configuration.GetValue<string>("RabbitMQ:UserName", "guest"),
                    Password = configuration.GetValue<string>("RabbitMQ:Password", "guest"),
                    VirtualHost = configuration.GetValue<string>("RabbitMQ:VirtualHost", "/"),
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                InitializeExchangesAndQueues();

                _logger.LogInformation("RabbitMQ conectado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao conectar ao RabbitMQ");
                throw;
            }
        }

        private void InitializeExchangesAndQueues()
        {
            try
            {
                _channel.ExchangeDeclare("ticket.events", ExchangeType.Topic, durable: true);
                _channel.ExchangeDeclare("ticket.events.dlx", ExchangeType.Topic, durable: true);
                _logger.LogInformation("Exchanges declaradas: ticket.events, ticket.events.dlx");

                var queues = new[]
                {
"reservation.created",
"reservation.confirmed",
"payment.failed",
"ticket.generated"
};

                foreach (var queue in queues)
                {
                    try
                    {
                        var args = new Dictionary<string, object>
{
{ "x-dead-letter-exchange", "ticket.events.dlx" },
{ "x-dead-letter-routing-key", queue + ".dlq" }
};

                        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: args);
                        _logger.LogInformation("Fila declarada: {Queue}", queue);

                        _channel.QueueBind(queue, "ticket.events", queue);
                        _logger.LogInformation("Binding criado: ticket.events -> {Queue} (routing key: {Queue})", queue, queue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao configurar fila {Queue}", queue);
                    }

                    var dlqName = queue + ".dlq";
                    try
                    {
                        _channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                        _channel.QueueBind(dlqName, "ticket.events.dlx", queue + ".dlq");
                        _logger.LogInformation("DLQ declarada: {DLQ}", dlqName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao declarar DLQ {DLQ}", dlqName);
                    }
                }

                _logger.LogInformation("Todas as filas e bindings configurados com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar exchanges e filas");
                throw;
            }
        }

        public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        {
            if (!_isEnabled)
            {
                _logger.LogDebug("RabbitMQ desabilitado. Evento ignorado: {EventType}", typeof(T).Name);
                return;
            }

            try
            {
                var eventName = typeof(T).Name;
                var routingKey = GetRoutingKey(eventName);
                var json = JsonSerializer.Serialize(@event, _jsonOptions);
                var body = Encoding.UTF8.GetBytes(json);

                _logger.LogInformation("Publicando evento: {EventType} | RoutingKey: {RoutingKey}", eventName, routingKey);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.ContentType = "application/json";
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.MessageId = Guid.NewGuid().ToString();

                _channel.BasicPublish("ticket.events", routingKey, properties, body);

                _logger.LogInformation("Evento publicado: {EventType} | RoutingKey: {RoutingKey} | MessageId: {MessageId}",
                eventName, routingKey, properties.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar evento: {EventType}", typeof(T).Name);
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task PublishWithRetryAsync<T>(T @event, int maxRetries = 3, CancellationToken cancellationToken = default) where T : class
        {
            if (!_isEnabled)
            {
                _logger.LogDebug("RabbitMQ desabilitado. Evento ignorado: {EventType}", typeof(T).Name);
                return;
            }

            var attempt = 0;
            var delay = _retryDelayMs;

            while (attempt < maxRetries)
            {
                try
                {
                    await PublishAsync(@event, cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogWarning(ex, "Falha ao publicar evento {EventType}. Tentativa {Attempt}/{MaxRetries}",
                    typeof(T).Name, attempt, maxRetries);

                    if (attempt >= maxRetries)
                    {
                        _logger.LogError(ex, "Falha apos {MaxRetries} tentativas ao publicar evento {EventType}",
                        maxRetries, typeof(T).Name);
                        throw;
                    }

                    await Task.Delay(delay * attempt, cancellationToken);
                }
            }
        }

        private static string GetRoutingKey(string eventName)
        {
            return eventName switch
            {
                nameof(ReservationCreatedEvent) => "reservation.created",
                nameof(ReservationConfirmedEvent) => "reservation.confirmed",
                nameof(PaymentFailedEvent) => "payment.failed",
                nameof(TicketGeneratedEvent) => "ticket.generated",
                _ => eventName.ToLowerInvariant().Replace("event", "").Trim('.')
            };
        }

        public void Dispose()
        {
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                    _channel.Dispose();
                }
                catch { }
            }

            if (_connection != null)
            {
                try
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                catch { }
            }
        }
    }
}
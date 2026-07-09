using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TicketSystem.Application.Events;

namespace TicketSystem.Infrastructure.Messaging.Consumers
{
    public class TicketGeneratedConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<TicketGeneratedConsumer> _logger;
        private readonly bool _isEnabled;
        private readonly JsonSerializerOptions _jsonOptions;

        public TicketGeneratedConsumer(
        IConfiguration configuration,
        ILogger<TicketGeneratedConsumer> logger)
        {
            _logger = logger;
            _isEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", false);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            if (!_isEnabled)
            {
                _logger.LogInformation("RabbitMQ desabilitado. Consumer desativado.");
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

                _channel.QueueDeclare("ticket.generated", durable: true, exclusive: false, autoDelete: false);

                _logger.LogInformation("TicketGeneratedConsumer inicializado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar TicketGeneratedConsumer");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_isEnabled)
            {
                return Task.CompletedTask;
            }

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var eventObj = JsonSerializer.Deserialize<TicketGeneratedEvent>(message, _jsonOptions);

                    if (eventObj == null)
                    {
                        _logger.LogWarning("Evento nulo recebido");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation("Bilhete gerado: {TicketCode} - {PassengerName} - {TripOrigin} -> {TripDestination}",
                    eventObj.TicketCode, eventObj.PassengerName, eventObj.TripOrigin, eventObj.TripDestination);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar ticket gerado");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("ticket.generated", false, consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            if (_channel != null)
            {
                _channel.Close();
                _channel.Dispose();
            }

            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
            }

            base.Dispose();
        }
    }
}
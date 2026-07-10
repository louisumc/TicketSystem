using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TicketSystem.Application.Events;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Messaging.Consumers
{
    public class ReservationCreatedConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<ReservationCreatedConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _isEnabled;
        private readonly JsonSerializerOptions _jsonOptions;

        public ReservationCreatedConsumer(
        IConfiguration configuration,
        ILogger<ReservationCreatedConsumer> logger,
        IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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

                var args = new Dictionary<string, object>
{
{ "x-dead-letter-exchange", "ticket.events.dlx" },
{ "x-dead-letter-routing-key", "reservation.created.dlq" }
};
                _channel.QueueDeclare("reservation.created", durable: true, exclusive: false, autoDelete: false, arguments: args);

                _logger.LogInformation("ReservationCreatedConsumer inicializado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar ReservationCreatedConsumer");
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

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var eventObj = JsonSerializer.Deserialize<ReservationCreatedEvent>(message, _jsonOptions);

                    if (eventObj == null)
                    {
                        _logger.LogWarning("Evento nulo recebido");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation("Processando reserva criada: {ReservationId} - {PassengerName}",
                    eventObj.ReservationId, eventObj.PassengerName);

                    await ProcessReservationCreatedAsync(eventObj, stoppingToken);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar reserva criada");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("reservation.created", false, consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessReservationCreatedAsync(ReservationCreatedEvent eventObj, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reserva {ReservationId} criada para {PassengerName} - Assentos: {Seats}",
            eventObj.ReservationId, eventObj.PassengerName, string.Join(", ", eventObj.SeatNumbers));

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            _logger.LogInformation("Reserva {ReservationId} processada com sucesso", eventObj.ReservationId);
        }

        public override void Dispose()
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

            base.Dispose();
        }
    }
}
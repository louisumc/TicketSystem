using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TicketSystem.Application.Events;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Infrastructure.Messaging.Consumers
{
    public class PaymentFailedConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<PaymentFailedConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _isEnabled;
        private readonly JsonSerializerOptions _jsonOptions;

        public PaymentFailedConsumer(
        IConfiguration configuration,
        ILogger<PaymentFailedConsumer> logger,
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

                _channel.QueueDeclare("payment.failed", durable: true, exclusive: false, autoDelete: false);

                _logger.LogInformation("PaymentFailedConsumer inicializado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar PaymentFailedConsumer");
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
                    var eventObj = JsonSerializer.Deserialize<PaymentFailedEvent>(message, _jsonOptions);

                    if (eventObj == null)
                    {
                        _logger.LogWarning("Evento nulo recebido");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation("Processando falha de pagamento: {ReservationId} - {FailureReason}",
                    eventObj.ReservationId, eventObj.FailureReason);

                    await ProcessPaymentFailedAsync(eventObj, stoppingToken);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar falha de pagamento");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("payment.failed", false, consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessPaymentFailedAsync(PaymentFailedEvent eventObj, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();

            var reservation = await reservationService.GetReservationByIdAsync(eventObj.ReservationId);
            if (reservation == null || reservation.Status != ReservationStatus.Pending)
            {
                _logger.LogWarning("Reserva nao encontrada ou status invalido: {ReservationId} - Status: {Status}",
                eventObj.ReservationId, reservation?.Status);
                return;
            }

            await reservationService.CancelReservationAsync(eventObj.ReservationId);

            _logger.LogInformation("Reserva cancelada apos falha de pagamento: {ReservationId}", eventObj.ReservationId);
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
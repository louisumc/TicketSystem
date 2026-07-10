using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Events;
using TicketSystem.Application.Interfaces;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Infrastructure.Workers
{
    public class PaymentRetryWorker : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<PaymentRetryWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _isEnabled;
        private readonly int _maxRetries;
        private readonly JsonSerializerOptions _jsonOptions;

        public PaymentRetryWorker(
        IConfiguration configuration,
        ILogger<PaymentRetryWorker> logger,
        IServiceProvider serviceProvider,
        IConnection connection)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _isEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", false);
            _maxRetries = configuration.GetValue<int>("RabbitMQ:PaymentRetryMaxAttempts", 3);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            if (!_isEnabled)
            {
                _logger.LogInformation("RabbitMQ desabilitado. PaymentRetryWorker desativado.");
                return;
            }

            _connection = connection;
            _channel = _connection.CreateModel();

            _logger.LogInformation("PaymentRetryWorker inicializado");
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

                    _logger.LogInformation("Processando retry de pagamento: {ReservationId}",
                    eventObj.ReservationId);

                    await ProcessPaymentRetryAsync(eventObj, ea, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar retry de pagamento");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("payment.failed", false, consumer);

            return Task.CompletedTask;
        }

        private int GetAttemptCount(BasicDeliverEventArgs ea)
        {
            var headers = ea.BasicProperties.Headers;
            if (headers != null && headers.TryGetValue("x-retry-count", out var countObj))
            {
                return Convert.ToInt32(countObj);
            }
            return 0;
        }

        private async Task ProcessPaymentRetryAsync(PaymentFailedEvent eventObj, BasicDeliverEventArgs ea, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();
            var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var currentAttempt = GetAttemptCount(ea) + 1;

            if (currentAttempt > _maxRetries)
            {
                _logger.LogWarning("Maximo de tentativas atingido para reserva: {ReservationId}. Cancelando reserva.", eventObj.ReservationId);

                await reservationService.CancelReservationAsync(eventObj.ReservationId);
                _channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            _logger.LogInformation("Tentativa {Attempt}/{MaxRetries} de pagamento para reserva: {ReservationId}",
            currentAttempt, _maxRetries, eventObj.ReservationId);

            var delay = TimeSpan.FromSeconds(Math.Pow(2, currentAttempt) * 5);
            _logger.LogDebug("Aguardando {Delay} segundos antes de tentar novamente", delay.TotalSeconds);
            await Task.Delay(delay, cancellationToken);

            var paymentResult = await paymentService.ProcessPaymentAsync(
            eventObj.ReservationId,
            eventObj.PaymentMethod,
            eventObj.TotalAmount,
            cancellationToken);

            if (paymentResult.Success)
            {
                var confirmDto = new ConfirmReservationDto
                {
                    ReservationId = eventObj.ReservationId,
                    PaymentMethod = eventObj.PaymentMethod,
                    Observations = "Retry " + currentAttempt + " - Transacao: " + paymentResult.TransactionId
                };

                var confirmed = await reservationService.ConfirmReservationAsync(confirmDto);

                _logger.LogInformation("Pagamento confirmado apos retry para reserva: {ReservationId}", eventObj.ReservationId);
                _channel.BasicAck(ea.DeliveryTag, false);

                var confirmedEvent = new ReservationConfirmedEvent
                {
                    ReservationId = confirmed.Id,
                    TripId = confirmed.TripId,
                    PassengerId = confirmed.PassengerId,
                    PassengerName = confirmed.PassengerName,
                    PassengerEmail = confirmed.PassengerEmail,
                    PassengerDocument = confirmed.PassengerDocument,
                    Seats = confirmed.Seats.Select(s => new SeatInfo
                    {
                        Number = s.SeatNumber,
                        Type = s.SeatType.ToString(),
                        Price = s.Price,
                        Row = s.Row,
                        Column = s.Column
                    }).ToList(),
                    TotalAmount = confirmed.TotalAmount,
                    ConfirmedAt = DateTime.UtcNow,
                    PaymentMethod = eventObj.PaymentMethod,
                    TripOrigin = confirmed.TripOrigin,
                    TripDestination = confirmed.TripDestination,
                    TripDepartureTime = confirmed.TripDepartureTime
                };

                await eventPublisher.PublishAsync(confirmedEvent, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Falha no retry {Attempt} para reserva: {ReservationId} - {Reason}",
                currentAttempt, eventObj.ReservationId, paymentResult.FailureReason);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Headers = new Dictionary<string, object>
{
{ "x-retry-count", currentAttempt }
};
                properties.ContentType = "application/json";
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                var body = ea.Body.ToArray();

                _channel.BasicPublish("ticket.events", "payment.failed", properties, body);
                _channel.BasicAck(ea.DeliveryTag, false);

                _logger.LogInformation("Mensagem reenfileirada para nova tentativa: {ReservationId}", eventObj.ReservationId);
            }
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
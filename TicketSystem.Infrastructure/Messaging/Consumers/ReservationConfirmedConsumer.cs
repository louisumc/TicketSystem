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

namespace TicketSystem.Infrastructure.Messaging.Consumers
{
    public class ReservationConfirmedConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<ReservationConfirmedConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _isEnabled;
        private readonly JsonSerializerOptions _jsonOptions;

        public ReservationConfirmedConsumer(
        IConfiguration configuration,
        ILogger<ReservationConfirmedConsumer> logger,
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
{ "x-dead-letter-routing-key", "reservation.confirmed.dlq" }
};
                _channel.QueueDeclare("reservation.confirmed", durable: true, exclusive: false, autoDelete: false, arguments: args);

                _logger.LogInformation("ReservationConfirmedConsumer inicializado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inicializar ReservationConfirmedConsumer");
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
                    var eventObj = JsonSerializer.Deserialize<ReservationConfirmedEvent>(message, _jsonOptions);

                    if (eventObj == null)
                    {
                        _logger.LogWarning("Evento nulo recebido");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation("Processando confirmacao de reserva: {ReservationId}", eventObj.ReservationId);

                    await ProcessReservationConfirmedAsync(eventObj, stoppingToken);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar confirmacao de reserva");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("reservation.confirmed", false, consumer);

            return Task.CompletedTask;
        }

        private async Task ProcessReservationConfirmedAsync(ReservationConfirmedEvent eventObj, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Gerando bilhete para reserva: {ReservationId}", eventObj.ReservationId);

            var ticketCode = GenerateTicketCode(eventObj);
            var qrCode = GenerateQrCode(ticketCode);

            var basePath = Directory.GetCurrentDirectory();
            var ticketsPath = Path.Combine(basePath, "wwwroot", "tickets");

            if (!Directory.Exists(ticketsPath))
            {
                Directory.CreateDirectory(ticketsPath);
                _logger.LogInformation("Diretorio criado: {TicketsPath}", ticketsPath);
            }

            var ticketPath = Path.Combine(ticketsPath, ticketCode + ".txt");
            var ticketContent = GenerateTicketContent(eventObj, ticketCode, qrCode);

            await File.WriteAllTextAsync(ticketPath, ticketContent, cancellationToken);

            if (File.Exists(ticketPath))
            {
                _logger.LogInformation("Bilhete TXT criado: {TicketPath}", ticketPath);
            }
            else
            {
                _logger.LogWarning("Falha ao criar bilhete TXT: {TicketPath}", ticketPath);
            }

            _logger.LogInformation("Bilhete gerado: {TicketCode} - {TicketPath}", ticketCode, ticketPath);

            using var scope = _serviceProvider.CreateScope();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var ticketEvent = new TicketGeneratedEvent
            {
                ReservationId = eventObj.ReservationId,
                TicketCode = ticketCode,
                PassengerName = eventObj.PassengerName,
                PassengerEmail = eventObj.PassengerEmail,
                PassengerDocument = eventObj.PassengerDocument,
                Seats = eventObj.Seats.Select(s => new TicketSeatInfo
                {
                    Number = s.Number,
                    Type = s.Type,
                    Price = s.Price,
                    Row = s.Row,
                    Column = s.Column
                }).ToList(),
                TripOrigin = eventObj.TripOrigin,
                TripDestination = eventObj.TripDestination,
                TripDepartureTime = eventObj.TripDepartureTime,
                GeneratedAt = DateTime.UtcNow,
                QrCode = qrCode
            };

            await eventPublisher.PublishAsync(ticketEvent, cancellationToken);

            _logger.LogInformation("TicketGeneratedEvent publicado para reserva: {ReservationId}", eventObj.ReservationId);
        }

        private string GenerateTicketCode(ReservationConfirmedEvent eventObj)
        {
            var now = DateTime.UtcNow;
            var prefix = "TKT";
            var date = now.ToString("yyMMdd");
            var random = new Random().Next(1000, 9999);
            var hash = eventObj.ReservationId.ToString().Substring(0, 6);
            return prefix + "-" + date + "-" + random + "-" + hash;
        }

        private string GenerateQrCode(string ticketCode)
        {
            return "QR-" + ticketCode + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private string GenerateTicketContent(ReservationConfirmedEvent eventObj, string ticketCode, string qrCode)
        {
            return @"
=== BILHETE DE VIAGEM ===

Ticket: " + ticketCode + @"
QR Code: " + qrCode + @"
Passageiro: " + eventObj.PassengerName + @"
Documento: " + eventObj.PassengerDocument + @"

Viagem: " + eventObj.TripOrigin + " -> " + eventObj.TripDestination + @"
Data: " + eventObj.TripDepartureTime + @"

Assentos:
" + string.Join("\n", eventObj.Seats.Select(s => " - " + s.Number + " (" + s.Type + ") - R$ " + s.Price.ToString("F2"))) + @"

Total: R$ " + eventObj.TotalAmount.ToString("F2") + @"

==========================
Este bilhete e eletronico.
Apresente no embarque.
";
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
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

namespace TicketSystem.Infrastructure.Workers
{
    public class TicketGenerationWorker : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<TicketGenerationWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _isEnabled;
        private readonly JsonSerializerOptions _jsonOptions;

        public TicketGenerationWorker(
            IConfiguration configuration,
            ILogger<TicketGenerationWorker> logger,
            IServiceProvider serviceProvider,
            IConnection connection)
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
                _logger.LogInformation("RabbitMQ desabilitado. TicketGenerationWorker desativado.");
                return;
            }

            _connection = connection;
            _channel = _connection.CreateModel();

            // Configurar QoS para processar apenas 1 mensagem por vez (evitar sobrecarga)
            _channel.BasicQos(0, 1, false);

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "ticket.events.dlx" },
                { "x-dead-letter-routing-key", "reservation.confirmed.dlq" }
            };
            _channel.QueueDeclare("reservation.confirmed", durable: true, exclusive: false, autoDelete: false, arguments: args);

            _logger.LogInformation("TicketGenerationWorker inicializado. Escutando fila: reservation.confirmed");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_isEnabled)
            {
                return Task.CompletedTask;
            }

            _logger.LogInformation("TicketGenerationWorker iniciando consumo da fila reservation.confirmed...");

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

                    _logger.LogInformation("Processando geracao de bilhete para reserva: {ReservationId}", eventObj.ReservationId);

                    await GenerateTicketAsync(eventObj, stoppingToken);

                    // ACK apenas após processamento bem-sucedido
                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Bilhete processado com sucesso para reserva: {ReservationId}", eventObj.ReservationId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar geracao de bilhete");
                    // Rejeita e reenvia (requeue = true)
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("reservation.confirmed", false, consumer);
            _logger.LogInformation("TicketGenerationWorker escutando fila reservation.confirmed");

            return Task.CompletedTask;
        }

        private async Task GenerateTicketAsync(ReservationConfirmedEvent eventObj, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Gerando bilhete para reserva: {ReservationId}", eventObj.ReservationId);

            // Pequeno delay para garantir que a transação foi commitada
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

            var ticketCode = GenerateTicketCode(eventObj);
            var ticketUrl = "/tickets/" + ticketCode + ".txt";

            var basePath = Directory.GetCurrentDirectory();
            var ticketsPath = Path.Combine(basePath, "wwwroot", "tickets");

            if (!Directory.Exists(ticketsPath))
            {
                Directory.CreateDirectory(ticketsPath);
                _logger.LogInformation("Diretorio criado: {TicketsPath}", ticketsPath);
            }

            var storagePath = Path.Combine(ticketsPath, ticketCode + ".txt");

            var content = GenerateTicketContent(eventObj, ticketCode);

            // Escreve o arquivo com retry
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromMilliseconds(500);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await File.WriteAllTextAsync(storagePath, content, cancellationToken);

                    // Verifica se o arquivo foi realmente criado
                    if (File.Exists(storagePath))
                    {
                        var fileInfo = new FileInfo(storagePath);
                        _logger.LogInformation("Bilhete TXT criado: {StoragePath} - Tamanho: {Size} bytes - Tentativa: {Attempt}",
                            storagePath, fileInfo.Length, attempt);
                        break;
                    }

                    if (attempt == maxRetries)
                    {
                        throw new IOException($"Arquivo não foi criado após {maxRetries} tentativas: {storagePath}");
                    }

                    _logger.LogWarning("Arquivo não encontrado após escrita, tentando novamente... Tentativa {Attempt}/{MaxRetries}",
                        attempt, maxRetries);
                    await Task.Delay(retryDelay * attempt, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao escrever arquivo {StoragePath} - Tentativa {Attempt}/{MaxRetries}",
                        storagePath, attempt, maxRetries);

                    if (attempt == maxRetries)
                    {
                        throw;
                    }

                    await Task.Delay(retryDelay * attempt, cancellationToken);
                }
            }

            _logger.LogInformation("Bilhete gerado: {TicketCode} - {TicketUrl}", ticketCode, ticketUrl);

            using var scope = _serviceProvider.CreateScope();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var qrCode = GenerateQrCode(ticketCode);

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

            // Publicar com retry
            await eventPublisher.PublishWithRetryAsync(ticketEvent, 3, cancellationToken);

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

        private string GenerateTicketContent(ReservationConfirmedEvent eventObj, string ticketCode)
        {
            return @"
=== BILHETE DE VIAGEM ===

Ticket: " + ticketCode + @"
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
// FILENAME: TicketSystem.Infrastructure/Workers/EmailNotificationWorker.cs
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
    public class EmailNotificationWorker : BackgroundService
    {
        private readonly ILogger<EmailNotificationWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _isEnabled;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IModel _channel;
        private readonly IConnection _connection;
        private readonly int _maxRetries;

        public EmailNotificationWorker(
            IConfiguration configuration,
            ILogger<EmailNotificationWorker> logger,
            IServiceProvider serviceProvider,
            IConnection connection)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _isEnabled = configuration.GetValue<bool>("RabbitMQ:Enabled", false);
            _maxRetries = configuration.GetValue<int>("Email:MaxRetries", 3);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            if (!_isEnabled)
            {
                _logger.LogInformation("RabbitMQ desabilitado. EmailNotificationWorker desativado.");
                return;
            }

            _connection = connection;
            _channel = _connection.CreateModel();

            // QoS - processar apenas 1 mensagem por vez
            _channel.BasicQos(0, 1, false);

            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "ticket.events.dlx" },
                { "x-dead-letter-routing-key", "ticket.generated.dlq" }
            };
            _channel.QueueDeclare("ticket.generated", durable: true, exclusive: false, autoDelete: false, arguments: args);

            _logger.LogInformation("EmailNotificationWorker inicializado. Escutando fila: ticket.generated");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_isEnabled)
            {
                return;
            }

            _logger.LogInformation("EmailNotificationWorker iniciando consumo da fila ticket.generated...");

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
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

                    _logger.LogInformation("Processando envio de email para: {Email} - Ticket: {TicketCode}",
                        eventObj.PassengerEmail, eventObj.TicketCode);

                    await SendEmailWithRetryAsync(eventObj, _maxRetries, stoppingToken);

                    _channel.BasicAck(ea.DeliveryTag, false);
                    _logger.LogInformation("Email processado com sucesso para: {Email}", eventObj.PassengerEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar envio de email");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("ticket.generated", false, consumer);
            _logger.LogInformation("EmailNotificationWorker escutando fila ticket.generated");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task SendEmailWithRetryAsync(TicketGeneratedEvent eventObj, int maxRetries, CancellationToken cancellationToken)
        {
            var attempt = 0;
            var baseDelay = TimeSpan.FromSeconds(1);

            while (attempt < maxRetries)
            {
                try
                {
                    await SendEmailAsync(eventObj, cancellationToken);
                    return;
                }
                catch (Exception ex)
                {
                    attempt++;
                    _logger.LogWarning(ex, "Falha ao enviar email para {Email} - Tentativa {Attempt}/{MaxRetries}",
                        eventObj.PassengerEmail, attempt, maxRetries);

                    if (attempt >= maxRetries)
                    {
                        _logger.LogError(ex, "Falha apos {MaxRetries} tentativas ao enviar email para {Email}",
                            maxRetries, eventObj.PassengerEmail);
                        throw;
                    }

                    var delay = baseDelay * (int)Math.Pow(2, attempt - 1);
                    _logger.LogDebug("Aguardando {Delay}ms antes da proxima tentativa", delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        private async Task SendEmailAsync(TicketGeneratedEvent eventObj, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Enviando email para {Email} com bilhete {TicketCode}",
                eventObj.PassengerEmail, eventObj.TicketCode);

            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var subject = "Seu bilhete de viagem - " + eventObj.TicketCode;

            var body = $@"
<html>
<head><meta charset='UTF-8'></head>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #2c3e50;'>Ticket System - Bilhete de Viagem</h2>
    <hr/>
    <p><strong>Passageiro:</strong> {eventObj.PassengerName}</p>
    <p><strong>Ticket:</strong> {eventObj.TicketCode}</p>
    <p><strong>QR Code:</strong> {eventObj.QrCode}</p>
    <p><strong>Viagem:</strong> {eventObj.TripOrigin} ➜ {eventObj.TripDestination}</p>
    <p><strong>Partida:</strong> {eventObj.TripDepartureTime:dd/MM/yyyy HH:mm}</p>
    <p><strong>Assentos:</strong> {string.Join(", ", eventObj.Seats.Select(s => s.Number))}</p>
    <hr/>
    <p><em>Este é um bilhete eletrônico. Apresente no embarque.</em></p>
    <p><em>O arquivo do bilhete está em anexo.</em></p>
</body>
</html>";

            var basePath = Directory.GetCurrentDirectory();
            var attachmentPath = Path.Combine(basePath, "wwwroot", "tickets", eventObj.TicketCode + ".txt");

            // Verifica se o arquivo existe, se não, tenta criar
            if (!File.Exists(attachmentPath))
            {
                _logger.LogWarning("Arquivo de anexo nao encontrado: {AttachmentPath}. Tentando criar...", attachmentPath);

                // Tenta criar o arquivo a partir dos dados do evento
                var content = GenerateTicketContent(eventObj);

                var ticketsPath = Path.Combine(basePath, "wwwroot", "tickets");
                if (!Directory.Exists(ticketsPath))
                {
                    Directory.CreateDirectory(ticketsPath);
                }

                await File.WriteAllTextAsync(attachmentPath, content, cancellationToken);
                _logger.LogInformation("Arquivo de ticket recriado: {AttachmentPath}", attachmentPath);
            }

            if (File.Exists(attachmentPath))
            {
                var fileInfo = new FileInfo(attachmentPath);
                _logger.LogInformation("Anexo encontrado: {AttachmentPath} - Tamanho: {Size} bytes",
                    attachmentPath, fileInfo.Length);
            }
            else
            {
                _logger.LogWarning("Anexo nao encontrado apos tentativa de criacao: {AttachmentPath}", attachmentPath);
            }

            await emailService.SendTicketEmailAsync(
                eventObj.PassengerEmail,
                subject,
                body,
                File.Exists(attachmentPath) ? attachmentPath : null
            );

            _logger.LogInformation("Email enviado com sucesso para {Email}", eventObj.PassengerEmail);
        }

        private string GenerateTicketContent(TicketGeneratedEvent eventObj)
        {
            return @"
=== BILHETE DE VIAGEM ===

Ticket: " + eventObj.TicketCode + @"
QR Code: " + eventObj.QrCode + @"
Passageiro: " + eventObj.PassengerName + @"
Documento: " + eventObj.PassengerDocument + @"

Viagem: " + eventObj.TripOrigin + " -> " + eventObj.TripDestination + @"
Data: " + eventObj.TripDepartureTime + @"

Assentos:
" + string.Join("\n", eventObj.Seats.Select(s => " - " + s.Number + " (" + s.Type + ") - R$ " + s.Price.ToString("F2"))) + @"

Total: R$ " + eventObj.Seats.Sum(s => s.Price).ToString("F2") + @"

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
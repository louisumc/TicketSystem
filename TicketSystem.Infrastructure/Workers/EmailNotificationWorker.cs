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
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<EmailNotificationWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly bool _isEnabled;
        private readonly JsonSerializerOptions _jsonOptions;

        public EmailNotificationWorker(
        IConfiguration configuration,
        ILogger<EmailNotificationWorker> logger,
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
                _logger.LogInformation("RabbitMQ desabilitado. EmailNotificationWorker desativado.");
                return;
            }

            _connection = connection;
            _channel = _connection.CreateModel();

            _logger.LogInformation("EmailNotificationWorker inicializado");
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
                    var eventObj = JsonSerializer.Deserialize<TicketGeneratedEvent>(message, _jsonOptions);

                    if (eventObj == null)
                    {
                        _logger.LogWarning("Evento nulo recebido");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        return;
                    }

                    _logger.LogInformation("Processando envio de email para: {Email} - Ticket: {TicketCode}",
                    eventObj.PassengerEmail, eventObj.TicketCode);

                    await SendEmailAsync(eventObj);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar envio de email");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume("ticket.generated", false, consumer);

            return Task.CompletedTask;
        }

        private async Task SendEmailAsync(TicketGeneratedEvent eventObj)
        {
            _logger.LogInformation("Enviando email para {Email} com bilhete {TicketCode}",
            eventObj.PassengerEmail, eventObj.TicketCode);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var subject = "Seu bilhete de viagem - " + eventObj.TicketCode;
                var body = $@"

<h2>Ticket System - Bilhete de Viagem</h2> <p><strong>Passageiro:</strong> {eventObj.PassengerName}</p> <p><strong>Ticket:</strong> {eventObj.TicketCode}</p> <p><strong>Viagem:</strong> {eventObj.TripOrigin} → {eventObj.TripDestination}</p> <p><strong>Partida:</strong> {eventObj.TripDepartureTime}</p> <p><strong>Assentos:</strong> {string.Join(", ", eventObj.Seats.Select(s => s.Number))}</p> <p><strong>QR Code:</strong> {eventObj.QrCode}</p> <hr/> <p><em>Este e um bilhete eletronico. Apresente no embarque.</em></p> ";

                var attachmentPath = Path.Combine("wwwroot", "tickets", eventObj.TicketCode + ".pdf");

                await emailService.SendTicketEmailAsync(
                eventObj.PassengerEmail,
                subject,
                body,
                attachmentPath
                );

                _logger.LogInformation("Email enviado com sucesso para {Email}", eventObj.PassengerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar email para {Email}", eventObj.PassengerEmail);
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
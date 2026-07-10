using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly SmtpClient _smtpClient;
        private readonly string _from;
        private readonly string _fromName;
        private readonly bool _isEnabled;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _logger = logger;
            _isEnabled = configuration.GetValue<bool>("Email:Enabled", false);

            if (!_isEnabled)
            {
                _logger.LogInformation("Servico de email desabilitado");
                return;
            }

            var host = configuration.GetValue<string>("Email:Smtp:Host", "localhost");
            var port = configuration.GetValue<int>("Email:Smtp:Port", 25);
            var username = configuration.GetValue<string>("Email:Smtp:Username");
            var password = configuration.GetValue<string>("Email:Smtp:Password");
            _from = configuration.GetValue<string>("Email:Smtp:From", "ticketsystem@localhost");
            _fromName = configuration.GetValue<string>("Email:Smtp:FromName", "Ticket System");

            _smtpClient = new SmtpClient
            {
                Host = host,
                Port = port,
                EnableSsl = false,
                Credentials = string.IsNullOrEmpty(username) ? null : new NetworkCredential(username, password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30000
            };

            _logger.LogInformation("Servico SMTP configurado: {Host}:{Port}", host, port);
        }

        public async Task SendTicketEmailAsync(string to, string subject, string body, string attachmentPath, CancellationToken cancellationToken = default)
        {
            if (!_isEnabled)
            {
                _logger.LogInformation("Email desabilitado. Nao enviando para: {To}", to);
                return;
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_from, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                {
                    var attachment = new Attachment(attachmentPath);
                    message.Attachments.Add(attachment);
                    _logger.LogInformation("Anexo adicionado: {AttachmentPath}", attachmentPath);
                }
                else
                {
                    _logger.LogWarning("Arquivo de anexo nao encontrado: {AttachmentPath}", attachmentPath);
                }

                _logger.LogInformation("Enviando email para: {To}", to);
                await _smtpClient.SendMailAsync(message, cancellationToken);
                _logger.LogInformation("Email enviado com sucesso para: {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email para: {To}", to);
                throw;
            }
        }

        public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            await SendTicketEmailAsync(to, subject, body, null, cancellationToken);
        }
    }
}
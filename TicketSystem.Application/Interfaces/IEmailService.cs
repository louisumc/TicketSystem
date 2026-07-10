namespace TicketSystem.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendTicketEmailAsync(string to, string subject, string body, string attachmentPath, CancellationToken cancellationToken = default);
        Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
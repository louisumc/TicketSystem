namespace TicketSystem.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(Guid reservationId, string paymentMethod, decimal amount, CancellationToken cancellationToken = default);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string FailureReason { get; set; } = string.Empty;
    }
}
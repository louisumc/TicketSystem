using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TicketSystem.Application.Interfaces;

namespace TicketSystem.Infrastructure.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly double _successRate;
        private readonly int _delayMs;
        private readonly Random _random;

        public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger)
        {
            _logger = logger;
            _successRate = configuration.GetValue<double>("Payment:SuccessRate", 0.8);
            _delayMs = configuration.GetValue<int>("Payment:DelayMs", 1000);
            _random = new Random();
        }

        public async Task<PaymentResult> ProcessPaymentAsync(Guid reservationId, string paymentMethod, decimal amount, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processando pagamento para reserva: {ReservationId} - Metodo: {PaymentMethod} - Valor: {Amount}",
            reservationId, paymentMethod, amount);

            await Task.Delay(_random.Next(_delayMs / 2, _delayMs * 2), cancellationToken);

            var isSuccess = _random.NextDouble() < _successRate;

            if (isSuccess)
            {
                _logger.LogInformation("Pagamento aprovado para reserva: {ReservationId}", reservationId);

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = "TXN-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + _random.Next(10000, 99999),
                    Message = "Pagamento aprovado com sucesso",
                    ProcessedAt = DateTime.UtcNow
                };
            }

            var failureReasons = new[]
            {
"Saldo insuficiente",
"Cartao recusado",
"Tempo limite excedido",
"Erro no processador de pagamento",
"Falha na comunicacao com o banco"
};

            var failureReason = failureReasons[_random.Next(failureReasons.Length)];

            _logger.LogWarning("Pagamento recusado para reserva: {ReservationId} - Motivo: {FailureReason}",
            reservationId, failureReason);

            return new PaymentResult
            {
                Success = false,
                TransactionId = "TXN-FAIL-" + DateTime.UtcNow.ToString("yyMMdd") + "-" + _random.Next(10000, 99999),
                Message = "Pagamento recusado: " + failureReason,
                ProcessedAt = DateTime.UtcNow,
                FailureReason = failureReason
            };
        }
    }
}
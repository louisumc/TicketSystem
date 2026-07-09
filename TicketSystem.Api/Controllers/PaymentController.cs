using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Responses;

namespace TicketSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IReservationService _reservationService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
        IPaymentService paymentService,
        IReservationService reservationService,
        IEventPublisher eventPublisher,
        ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _reservationService = reservationService;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        [HttpPost("reservations/{id}/pay")]
        public async Task<IActionResult> ProcessPayment(Guid id, [FromBody] PaymentRequest request)
        {
            _logger.LogInformation("Recebendo requisicao de pagamento para reserva: {ReservationId}", id);

            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null)
            {
                return NotFound(new ApiResponse<object>(null, "Reserva nao encontrada"));
            }

            if (reservation.Status != Domain.Enums.ReservationStatus.Pending)
            {
                return BadRequest(new ApiResponse<object>(null, "Reserva nao esta pendente para pagamento"));
            }

            if (reservation.ExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new ApiResponse<object>(null, "Reserva expirada"));
            }

            var result = await _paymentService.ProcessPaymentAsync(id, request.PaymentMethod, reservation.TotalAmount);

            if (result.Success)
            {
                var confirmDto = new ConfirmReservationDto
                {
                    ReservationId = id,
                    PaymentMethod = request.PaymentMethod,
                    Observations = "Transacao: " + result.TransactionId
                };

                var confirmed = await _reservationService.ConfirmReservationAsync(confirmDto);

                _logger.LogInformation("Pagamento confirmado para reserva: {ReservationId}", id);

                return Ok(new ApiResponse<object>(new
                {
                    Reservation = confirmed,
                    Payment = result
                }, "Pagamento processado com sucesso"));
            }
            else
            {
                _logger.LogWarning("Pagamento falhou para reserva: {ReservationId} - {FailureReason}",
                id, result.FailureReason);

                return BadRequest(new ApiResponse<object>(new
                {
                    Reservation = reservation,
                    Payment = result
                }, "Pagamento recusado: " + result.FailureReason));
            }
        }
    }

    public class PaymentRequest
    {
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.DTOs.Reservation;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Responses;

namespace TicketSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reservations = await _reservationService.GetAllReservationsAsync();
            return Ok(new ApiResponse<IEnumerable<ReservationDto>>(reservations, "Reservas listadas com sucesso"));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationDto createDto)
        {
            var reservation = await _reservationService.CreateReservationAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = reservation.Id },
            new ApiResponse<ReservationDto>(reservation, "Reserva criada com sucesso"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);
            return Ok(new ApiResponse<ReservationDto>(reservation, "Reserva encontrada com sucesso"));
        }

        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmReservationDto confirmDto)
        {
            confirmDto.ReservationId = id;
            var reservation = await _reservationService.ConfirmReservationAsync(confirmDto);
            return Ok(new ApiResponse<ReservationDto>(reservation, "Reserva confirmada com sucesso"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _reservationService.CancelReservationAsync(id);
            return Ok(new ApiResponse<object>(null, "Reserva cancelada com sucesso"));
        }

        [HttpGet("trip/{tripId}/available")]
        public async Task<IActionResult> GetAvailableSeats(Guid tripId)
        {
            var availableSeats = await _reservationService.GetAvailableSeatsAsync(tripId);
            return Ok(new ApiResponse<AvailableSeatsDto>(availableSeats, "Assentos disponiveis listados com sucesso"));
        }

        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetByTripId(Guid tripId)
        {
            var reservations = await _reservationService.GetReservationsByTripIdAsync(tripId);
            return Ok(new ApiResponse<IEnumerable<ReservationDto>>(reservations, "Reservas listadas com sucesso"));
        }

        [HttpGet("passenger/{document}")]
        public async Task<IActionResult> GetByPassengerDocument(string document)
        {
            var reservations = await _reservationService.GetReservationsByPassengerDocumentAsync(document);
            return Ok(new ApiResponse<IEnumerable<ReservationDto>>(reservations, "Reservas listadas com sucesso"));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.DTOs.Trip;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Responses;
using TicketSystem.Domain.Enums;

namespace TicketSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TripsController(ITripService tripService)
        {
            _tripService = tripService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var trips = await _tripService.GetAllTripResponsesAsync();
            return Ok(new ApiResponse<IEnumerable<TripResponseDto>>(trips, "Viagens listadas com sucesso"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var trip = await _tripService.GetTripResponseByIdAsync(id);
            return Ok(new ApiResponse<TripResponseDto>(trip, "Viagem encontrada com sucesso"));
        }

        [HttpGet("bus/{busId}")]
        public async Task<IActionResult> GetByBusId(Guid busId)
        {
            var trips = await _tripService.GetByBusIdAsync(busId);
            return Ok(new ApiResponse<IEnumerable<TripResponseDto>>(trips, "Viagens listadas com sucesso"));
        }

        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetByStatus(TripStatus status)
        {
            var trips = await _tripService.GetByStatusAsync(status);
            return Ok(new ApiResponse<IEnumerable<TripResponseDto>>(trips, "Viagens listadas com sucesso"));
        }

        [HttpGet("date-range")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var trips = await _tripService.GetByDateRangeAsync(startDate, endDate);
            return Ok(new ApiResponse<IEnumerable<TripResponseDto>>(trips, "Viagens listadas com sucesso"));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripDto createTripDto)
        {
            var trip = await _tripService.CreateTripAsync(createTripDto);
            return CreatedAtAction(nameof(GetById), new { id = trip.Id }, 
                new ApiResponse<TripResponseDto>(trip, "Viagem criada com sucesso"));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateTripDto updateTripDto)
        {
            var trip = await _tripService.UpdateTripAsync(updateTripDto);
            return Ok(new ApiResponse<TripResponseDto>(trip, "Viagem atualizada com sucesso"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _tripService.DeleteTripAsync(id);
            return Ok(new ApiResponse<object>(null, "Viagem removida com sucesso"));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] TripStatus status)
        {
            await _tripService.UpdateStatusAsync(id, status);
            return Ok(new ApiResponse<object>(null, "Status da viagem atualizado com sucesso"));
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.DTOs.Seat;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Responses;

namespace TicketSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatsController : ControllerBase
    {
        private readonly ISeatService _seatService;

        public SeatsController(ISeatService seatService)
        {
            _seatService = seatService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var seat = await _seatService.GetSeatByIdAsync(id);
            return Ok(new ApiResponse<SeatDto>(seat, "Assento encontrado com sucesso"));
        }

        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetByTripId(Guid tripId)
        {
            var seats = await _seatService.GetSeatsByTripIdAsync(tripId);
            return Ok(new ApiResponse<IEnumerable<SeatDto>>(seats, "Assentos listados com sucesso"));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSeatDto createSeatDto)
        {
            var seat = await _seatService.CreateSeatAsync(createSeatDto);
            return CreatedAtAction(nameof(GetById), new { id = seat.Id },
            new ApiResponse<SeatDto>(seat, "Assento criado com sucesso"));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateSeatDto updateSeatDto)
        {
            var seat = await _seatService.UpdateSeatAsync(updateSeatDto);
            return Ok(new ApiResponse<SeatDto>(seat, "Assento atualizado com sucesso"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _seatService.DeleteSeatAsync(id);
            return Ok(new ApiResponse<object>(null, "Assento removido com sucesso"));
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateSeatStatusDto updateDto)
        {
            var seat = await _seatService.UpdateSeatStatusAsync(id, updateDto);
            return Ok(new ApiResponse<SeatDto>(seat, "Status do assento atualizado com sucesso"));
        }

        [HttpGet("check-number/{tripId}/{number}")]
        public async Task<IActionResult> CheckNumber(Guid tripId, string number)
        {
            var exists = await _seatService.IsSeatNumberAvailableAsync(tripId, number);
            return Ok(new ApiResponse<bool>(!exists, exists ? "Número disponível" : "Número já utilizado"));
        }
    }
}
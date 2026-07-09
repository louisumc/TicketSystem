using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.DTOs.Passenger;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Responses;

namespace TicketSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PassengersController : ControllerBase
    {
        private readonly IPassengerService _passengerService;

        public PassengersController(IPassengerService passengerService)
        {
            _passengerService = passengerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var passengers = await _passengerService.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<PassengerDto>>(
            _passengerService.MapToDto(passengers),
            "Passageiros listados com sucesso"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var passenger = await _passengerService.GetByIdAsync(id);
            if (passenger == null)
                throw new KeyNotFoundException($"Passageiro com ID {id} não encontrado");

            return Ok(new ApiResponse<PassengerDto>(
            _passengerService.MapToDto(passenger),
            "Passageiro encontrado com sucesso"));
        }

        [HttpGet("document/{document}")]
        public async Task<IActionResult> GetByDocument(string document)
        {
            var passenger = await _passengerService.GetPassengerByDocumentAsync(document);
            return Ok(new ApiResponse<PassengerDto>(
            _passengerService.MapToDto(passenger),
            "Passageiro encontrado com sucesso"));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePassengerDto createDto)
        {
            var passenger = await _passengerService.CreatePassengerAsync(createDto);
            return CreatedAtAction(nameof(GetById), new { id = passenger.Id },
            new ApiResponse<PassengerDto>(passenger, "Passageiro criado com sucesso"));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdatePassengerDto updateDto)
        {
            var passenger = await _passengerService.UpdatePassengerAsync(updateDto);
            return Ok(new ApiResponse<PassengerDto>(passenger, "Passageiro atualizado com sucesso"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _passengerService.DeletePassengerAsync(id);
            return Ok(new ApiResponse<object>(null, "Passageiro removido com sucesso"));
        }
    }
}

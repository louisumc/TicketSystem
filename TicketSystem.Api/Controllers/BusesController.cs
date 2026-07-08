using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Application.Interfaces;
using TicketSystem.Application.Responses;

namespace TicketSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BusesController : ControllerBase
    {
        private readonly IBusService _busService;

        public BusesController(IBusService busService)
        {
            _busService = busService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var buses = await _busService.GetAllBusResponsesAsync();
            return Ok(new ApiResponse<IEnumerable<BusResponseDto>>(buses, "Ônibus listados com sucesso"));
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var buses = await _busService.GetActiveBusesAsync();
            return Ok(new ApiResponse<IEnumerable<BusResponseDto>>(buses, "Ônibus ativos listados com sucesso"));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var bus = await _busService.GetBusResponseByIdAsync(id);
            return Ok(new ApiResponse<BusResponseDto>(bus, "Ônibus encontrado com sucesso"));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBusDto createBusDto)
        {
            var bus = await _busService.CreateBusAsync(createBusDto);
            return CreatedAtAction(nameof(GetById), new { id = bus.Id }, 
                new ApiResponse<BusResponseDto>(bus, "Ônibus criado com sucesso"));
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateBusDto updateBusDto)
        {
            var bus = await _busService.UpdateBusAsync(updateBusDto);
            return Ok(new ApiResponse<BusResponseDto>(bus, "Ônibus atualizado com sucesso"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _busService.DeleteBusAsync(id);
            return Ok(new ApiResponse<object>(null, "Ônibus removido com sucesso"));
        }

        [HttpGet("check-plate/{plate}")]
        public async Task<IActionResult> CheckPlate(string plate)
        {
            var exists = await _busService.ExistsByPlateAsync(plate);
            return Ok(new ApiResponse<bool>(exists, exists ? "Placa já existe" : "Placa disponível"));
        }
    }
}
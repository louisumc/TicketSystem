using TicketSystem.Application.DTOs.Bus;
using TicketSystem.Domain.Entities;

namespace TicketSystem.Application.Interfaces
{
    public interface IBusService : IService<Bus>
    {
        // Métodos específicos de Bus
        Task<BusResponseDto> GetBusResponseByIdAsync(Guid id);
        Task<IEnumerable<BusResponseDto>> GetAllBusResponsesAsync();
        Task<IEnumerable<BusResponseDto>> GetActiveBusesAsync();
        Task<BusResponseDto> CreateBusAsync(CreateBusDto createBusDto);
        Task<BusResponseDto> UpdateBusAsync(UpdateBusDto updateBusDto);
        Task DeleteBusAsync(Guid id);
        Task<bool> ExistsByPlateAsync(string plate);
        Task<bool> ExistsByPlateAsync(string plate, Guid excludeId);
    }
}
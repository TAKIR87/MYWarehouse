using WarehouseAPI.DTOs.Counterparties;
using WarehouseAPI.Models;

namespace WarehouseAPI.Services.Interfaces;

public interface ICounterpartyService
{
    Task<IEnumerable<CounterpartyResponseDto>> GetAllAsync(CounterpartyType? type = null);
    Task<CounterpartyResponseDto?> GetByIdAsync(int id);
    Task<CounterpartyResponseDto> CreateAsync(CounterpartyCreateDto dto);
    Task<CounterpartyResponseDto?> UpdateAsync(int id, CounterpartyUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}
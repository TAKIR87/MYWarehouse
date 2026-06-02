using WarehouseAPI.DTOs.Stock;

namespace WarehouseAPI.Services.Interfaces;

public interface IStockService
{
    Task<IEnumerable<StockResponseDto>> GetAllAsync();
    Task<StockResponseDto?> GetByProductIdAsync(int productId);
}
using WarehouseAPI.DTOs.Analytics;

namespace WarehouseAPI.Services.Interfaces;

public interface IAnalyticsService
{
    Task<IEnumerable<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, int limit = 10);
    Task<TurnoverDto> GetTurnoverAsync(DateTime from, DateTime to);
    Task<IEnumerable<LowStockDto>> GetLowStockAsync(decimal minQuantity);
}
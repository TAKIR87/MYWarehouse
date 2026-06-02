using WarehouseAPI.Models;

namespace WarehouseAPI.Repositories.Interfaces;

public interface IAnalyticsRepository
{
    // Продажи сгруппированные по товарам за период
    Task<IEnumerable<(int ProductId, decimal TotalQuantity, decimal TotalAmount, int Count)>>
        GetTopProductsAsync(DateTime from, DateTime to, int limit);

    // Операции за период для расчёта оборотов
    Task<IEnumerable<Operation>> GetOperationsForTurnoverAsync(DateTime from, DateTime to);

    // Все остатки с товарами для проверки минимума
    Task<IEnumerable<Stock>> GetAllStocksWithProductsAsync();
}
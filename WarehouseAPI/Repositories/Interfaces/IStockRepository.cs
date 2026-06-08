using WarehouseAPI.Models;

namespace WarehouseAPI.Repositories.Interfaces;

public interface IStockRepository
{
    Task<IEnumerable<Stock>> GetAllAsync();
    Task<Stock?> GetByProductIdAsync(int productId);
}
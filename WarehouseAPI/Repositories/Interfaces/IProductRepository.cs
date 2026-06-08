using WarehouseAPI.Models;

namespace WarehouseAPI.Repositories.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<bool> ArticleExistsAsync(string article, int? excludeId = null);
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(Product product);
}
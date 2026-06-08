using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;

namespace WarehouseAPI.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.Stock)   // сразу подтягиваем остаток
            .AsNoTracking()          // только чтение — быстрее
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Stock)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Проверка уникальности артикула (excludeId нужен при обновлении)
    public async Task<bool> ArticleExistsAsync(string article, int? excludeId = null)
    {
        return await _context.Products
            .AnyAsync(p => p.Article == article && p.Id != excludeId);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task DeleteAsync(Product product)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }
}
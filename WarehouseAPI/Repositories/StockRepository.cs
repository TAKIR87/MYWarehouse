using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;

namespace WarehouseAPI.Repositories;

public class StockRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Stock>> GetAllAsync()
    {
        return await _context.Stocks
            .Include(s => s.Product)
            .AsNoTracking()
            .OrderBy(s => s.Product.Name)
            .ToListAsync();
    }

    public async Task<Stock?> GetByProductIdAsync(int productId)
    {
        return await _context.Stocks
            .Include(s => s.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProductId == productId);
    }
}
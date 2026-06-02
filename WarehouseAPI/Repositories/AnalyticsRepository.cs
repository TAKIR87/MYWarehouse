using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;

namespace WarehouseAPI.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly AppDbContext _context;

    public AnalyticsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<(int ProductId, decimal TotalQuantity, decimal TotalAmount, int Count)>>
        GetTopProductsAsync(DateTime from, DateTime to, int limit)
    {
        // Группируем строки продаж по товару за период
        var result = await _context.OperationItems
            .Where(oi =>
                oi.Operation.Type == OperationType.Sale &&
                oi.Operation.Date >= from &&
                oi.Operation.Date <= to)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId     = g.Key,
                TotalQuantity = g.Sum(oi => oi.Quantity),
                TotalAmount   = g.Sum(oi => oi.Quantity * oi.Price),
                Count         = g.Select(oi => oi.OperationId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalAmount)
            .Take(limit)
            .ToListAsync();

        return result.Select(x => (x.ProductId, x.TotalQuantity, x.TotalAmount, x.Count));
    }

    public async Task<IEnumerable<Operation>> GetOperationsForTurnoverAsync(DateTime from, DateTime to)
    {
        return await _context.Operations
            .Include(o => o.Items)
            .Where(o =>
                o.Date >= from &&
                o.Date <= to &&
                (o.Type == OperationType.Income ||
                 o.Type == OperationType.Sale   ||
                 o.Type == OperationType.WriteOff))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Stock>> GetAllStocksWithProductsAsync()
    {
        return await _context.Stocks
            .Include(s => s.Product)
            .AsNoTracking()
            .ToListAsync();
    }
}
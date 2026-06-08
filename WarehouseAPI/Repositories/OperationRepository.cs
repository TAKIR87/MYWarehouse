using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;

namespace WarehouseAPI.Repositories;

public class OperationRepository : IOperationRepository
{
    private readonly AppDbContext _context;

    public OperationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Operation>> GetHistoryAsync(
        DateTime? from,
        DateTime? to,
        OperationType? type,
        int? counterpartyId,
        int? productId,
        int page,
        int pageSize)
    {
        var query = _context.Operations
            .Include(o => o.Counterparty)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking();

        if (from.HasValue)
            query = query.Where(o => o.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(o => o.Date <= to.Value);

        if (type.HasValue)
            query = query.Where(o => o.Type == type.Value);

        if (counterpartyId.HasValue)
            query = query.Where(o => o.CounterpartyId == counterpartyId.Value);

        if (productId.HasValue)
            query = query.Where(o => o.Items.Any(i => i.ProductId == productId.Value));

        return await query
            .OrderByDescending(o => o.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Operation?> GetByIdAsync(int id)
    {
        return await _context.Operations
            .Include(o => o.Counterparty)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Operation> CreateAsync(Operation operation)
    {
        _context.Operations.Add(operation);
        await _context.SaveChangesAsync();
        return operation;
    }

    public async Task<Stock?> GetStockAsync(int productId)
    {
        return await _context.Stocks
            .FirstOrDefaultAsync(s => s.ProductId == productId);
    }

    public async Task UpdateStockAsync(Stock stock)
    {
        stock.UpdatedAt = DateTime.UtcNow;
        _context.Stocks.Update(stock);
        // SaveChanges вызывается снаружи в рамках транзакции
    }
}
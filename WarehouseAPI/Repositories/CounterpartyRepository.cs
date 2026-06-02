using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;

namespace WarehouseAPI.Repositories;

public class CounterpartyRepository : ICounterpartyRepository
{
    private readonly AppDbContext _context;

    public CounterpartyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Counterparty>> GetAllAsync(CounterpartyType? type = null)
    {
        var query = _context.Counterparties
            .Include(c => c.Contacts)
            .AsNoTracking();

        // Фильтрация по типу — если type не передан, вернём всех
        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Counterparty?> GetByIdAsync(int id)
    {
        return await _context.Counterparties
            .Include(c => c.Contacts)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Counterparty> CreateAsync(Counterparty counterparty)
    {
        _context.Counterparties.Add(counterparty);
        await _context.SaveChangesAsync();
        return counterparty;
    }

    public async Task<Counterparty> UpdateAsync(Counterparty counterparty)
    {
        _context.Counterparties.Update(counterparty);
        await _context.SaveChangesAsync();
        return counterparty;
    }

    public async Task DeleteAsync(Counterparty counterparty)
    {
        _context.Counterparties.Remove(counterparty);
        await _context.SaveChangesAsync();
    }
}
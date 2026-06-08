using WarehouseAPI.Models;

namespace WarehouseAPI.Repositories.Interfaces;

public interface ICounterpartyRepository
{
    Task<IEnumerable<Counterparty>> GetAllAsync(CounterpartyType? type = null);
    Task<Counterparty?> GetByIdAsync(int id);
    Task<Counterparty> CreateAsync(Counterparty counterparty);
    Task<Counterparty> UpdateAsync(Counterparty counterparty);
    Task DeleteAsync(Counterparty counterparty);
}
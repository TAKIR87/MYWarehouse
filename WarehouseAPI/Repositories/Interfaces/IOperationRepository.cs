using WarehouseAPI.Models;

namespace WarehouseAPI.Repositories.Interfaces;

public interface IOperationRepository
{
    Task<IEnumerable<Operation>> GetHistoryAsync(
        DateTime? from,
        DateTime? to,
        OperationType? type,
        int? counterpartyId,
        int? productId,
        int page,
        int pageSize);

    Task<Operation?> GetByIdAsync(int id);
    Task<Operation> CreateAsync(Operation operation);

    // Остатки — читаем и обновляем через этот же репозиторий
    Task<Stock?> GetStockAsync(int productId);
    Task UpdateStockAsync(Stock stock);
}
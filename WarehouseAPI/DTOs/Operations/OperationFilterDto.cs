using WarehouseAPI.Models;

namespace WarehouseAPI.DTOs.Operations;

// Параметры фильтрации для GET /api/operations/history
public class OperationFilterDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public OperationType? Type { get; set; }
    public int? CounterpartyId { get; set; }
    public int? ProductId { get; set; }

    // Пагинация
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
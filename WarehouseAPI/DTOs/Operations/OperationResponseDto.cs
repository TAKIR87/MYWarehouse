using WarehouseAPI.Models;

namespace WarehouseAPI.DTOs.Operations;

public class OperationResponseDto
{
    public int Id { get; set; }
    public OperationType Type { get; set; }
    public string TypeLabel => Type switch
    {
        OperationType.Income => "Приход",
        OperationType.Sale => "Реализация",
        OperationType.Transfer => "Перемещение",
        OperationType.WriteOff => "Списание",
        _ => "Неизвестно"
    };
    public DateTime Date { get; set; }
    public string? Comment { get; set; }

    // Контрагент — только нужные поля, не весь объект
    public int? CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }

    public List<OperationItemResponseDto> Items { get; set; } = new();

    // Итоговая сумма операции — удобно для истории
    public decimal TotalAmount => Items.Sum(i => i.Quantity * i.Price);
}

public class OperationItemResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductArticle { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
}
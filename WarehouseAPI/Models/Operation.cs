namespace WarehouseAPI.Models;

public enum OperationType { Income, Sale, Transfer, WriteOff }

public class Operation
{
    public int Id { get; set; }
    public OperationType Type { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
    public int? CounterpartyId { get; set; }    // поставщик/клиент

    public Counterparty? Counterparty { get; set; }
    public List<OperationItem> Items { get; set; } = new();
}
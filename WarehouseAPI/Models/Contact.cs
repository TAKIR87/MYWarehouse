namespace WarehouseAPI.Models;

public class Contact
{
    public int Id { get; set; }
    public int CounterpartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public Counterparty Counterparty { get; set; } = null!;
}
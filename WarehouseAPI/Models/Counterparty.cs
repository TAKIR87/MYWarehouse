namespace WarehouseAPI.Models;

public enum CounterpartyType { Client, Supplier, Company }

public class Counterparty
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CounterpartyType Type { get; set; }
    public string? Inn { get; set; }
    public string? Address { get; set; }

    public List<Contact> Contacts { get; set; } = new();
}
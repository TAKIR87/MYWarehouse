namespace WarehouseAPI.Models;

public class Stock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}
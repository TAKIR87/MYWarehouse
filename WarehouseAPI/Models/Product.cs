namespace WarehouseAPI.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;  // артикул
    public string Unit { get; set; } = string.Empty;     // ед. измерения: шт, кг, л
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Stock? Stock { get; set; }
}
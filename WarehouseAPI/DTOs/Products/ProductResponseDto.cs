namespace WarehouseAPI.DTOs.Products;

public class ProductResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal CurrentStock { get; set; }   // остаток — удобно сразу включить
    public DateTime CreatedAt { get; set; }
}
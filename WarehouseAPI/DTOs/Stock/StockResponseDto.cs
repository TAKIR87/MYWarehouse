namespace WarehouseAPI.DTOs.Stock;

public class StockResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductArticle { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalValue => Quantity * Price;  // сумма остатка
    public DateTime UpdatedAt { get; set; }
}
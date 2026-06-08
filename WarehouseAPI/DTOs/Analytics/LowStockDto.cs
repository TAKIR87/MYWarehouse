namespace WarehouseAPI.DTOs.Analytics;

public class LowStockDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductArticle { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinQuantity { get; set; }      // порог переданный в запросе
    public decimal Deficit => MinQuantity - CurrentQuantity;  // сколько не хватает
}
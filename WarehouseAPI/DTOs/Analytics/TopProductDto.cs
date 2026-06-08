namespace WarehouseAPI.DTOs.Analytics;

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductArticle { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }   // сколько продано
    public decimal TotalAmount { get; set; }      // на какую сумму
    public int OperationsCount { get; set; }      // в скольких продажах
}
namespace WarehouseAPI.Models;

public class OperationItem
{
	public int Id { get; set; }
	public int OperationId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public decimal Price { get; set; }

	public Operation Operation { get; set; } = null!;
	public Product Product { get; set; } = null!;
}
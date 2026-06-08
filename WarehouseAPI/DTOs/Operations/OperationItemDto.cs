using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.DTOs.Operations;

// Строка операции — используется и в Create, и в Response
public class OperationItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Range(0.001, double.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Цена не может быть отрицательной")]
    public decimal Price { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.DTOs.Products;

public class ProductCreateDto
{
    [Required(ErrorMessage = "Название обязательно")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Артикул обязателен")]
    [MaxLength(50)]
    public string Article { get; set; } = string.Empty;

    [Required(ErrorMessage = "Единица измерения обязательна")]
    [MaxLength(20)]
    public string Unit { get; set; } = string.Empty;  // шт, кг, л и т.д.

    [Range(0, double.MaxValue, ErrorMessage = "Цена не может быть отрицательной")]
    public decimal Price { get; set; }
}
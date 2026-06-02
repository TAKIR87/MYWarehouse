using System.ComponentModel.DataAnnotations;
using WarehouseAPI.Models;

namespace WarehouseAPI.DTOs.Operations;

public class IncomeCreateDto
{
    public int? CounterpartyId { get; set; }  // поставщик (необязательно)

    [MaxLength(500)]
    public string? Comment { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Укажите хотя бы один товар")]
    public List<OperationItemDto> Items { get; set; } = new();
}

public class SaleCreateDto
{
    public int? CounterpartyId { get; set; }  // клиент (необязательно)

    [MaxLength(500)]
    public string? Comment { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Укажите хотя бы один товар")]
    public List<OperationItemDto> Items { get; set; } = new();
}

public class TransferCreateDto
{
    [MaxLength(500)]
    public string? Comment { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Укажите хотя бы один товар")]
    public List<OperationItemDto> Items { get; set; } = new();
}

public class WriteOffCreateDto
{
    [MaxLength(500)]
    public string? Comment { get; set; }  // причина списания

    [Required]
    [MinLength(1, ErrorMessage = "Укажите хотя бы один товар")]
    public List<OperationItemDto> Items { get; set; } = new();
}
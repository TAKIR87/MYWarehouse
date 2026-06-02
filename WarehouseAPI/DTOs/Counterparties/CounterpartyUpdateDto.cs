using System.ComponentModel.DataAnnotations;
using WarehouseAPI.Models;

namespace WarehouseAPI.DTOs.Counterparties;

public class CounterpartyUpdateDto
{
    [Required(ErrorMessage = "Название обязательно")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Тип контрагента обязателен")]
    public CounterpartyType Type { get; set; }

    [MaxLength(12)]
    public string? Inn { get; set; }

    [MaxLength(300)]
    public string? Address { get; set; }

    public List<ContactDto> Contacts { get; set; } = new();
}
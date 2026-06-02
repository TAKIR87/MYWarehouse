using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.DTOs.Counterparties;

public class ContactDto
{
    public int? Id { get; set; }   // null = новый контакт, число = обновить существующий

    [Required(ErrorMessage = "Имя контакта обязательно")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Некорректный номер телефона")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string? Email { get; set; }
}
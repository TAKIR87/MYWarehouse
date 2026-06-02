using WarehouseAPI.Models;

namespace WarehouseAPI.DTOs.Counterparties;

public class CounterpartyResponseDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public CounterpartyType Type { get; set; }
	public string TypeLabel => Type switch         // удобно для отображения на UI
	{
		CounterpartyType.Client => "Клиент",
		CounterpartyType.Supplier => "Поставщик",
		CounterpartyType.Company => "Компания",
		_ => "Неизвестно"
	};
	public string? Inn { get; set; }
	public string? Address { get; set; }
	public List<ContactResponseDto> Contacts { get; set; } = new();
}

public class ContactResponseDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? Phone { get; set; }
	public string? Email { get; set; }
}
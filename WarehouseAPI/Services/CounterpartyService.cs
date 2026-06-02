using WarehouseAPI.DTOs.Counterparties;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Services;

public class CounterpartyService : ICounterpartyService
{
    private readonly ICounterpartyRepository _counterpartyRepository;

    public CounterpartyService(ICounterpartyRepository counterpartyRepository)
    {
        _counterpartyRepository = counterpartyRepository;
    }

    public async Task<IEnumerable<CounterpartyResponseDto>> GetAllAsync(CounterpartyType? type = null)
    {
        var counterparties = await _counterpartyRepository.GetAllAsync(type);
        return counterparties.Select(MapToResponse);
    }

    public async Task<CounterpartyResponseDto?> GetByIdAsync(int id)
    {
        var counterparty = await _counterpartyRepository.GetByIdAsync(id);
        return counterparty is null ? null : MapToResponse(counterparty);
    }

    public async Task<CounterpartyResponseDto> CreateAsync(CounterpartyCreateDto dto)
    {
        var counterparty = new Counterparty
        {
            Name    = dto.Name,
            Type    = dto.Type,
            Inn     = dto.Inn,
            Address = dto.Address,
            Contacts = dto.Contacts.Select(c => new Contact
            {
                Name  = c.Name,
                Phone = c.Phone,
                Email = c.Email
            }).ToList()
        };

        var created = await _counterpartyRepository.CreateAsync(counterparty);
        return MapToResponse(created);
    }

    public async Task<CounterpartyResponseDto?> UpdateAsync(int id, CounterpartyUpdateDto dto)
    {
        var counterparty = await _counterpartyRepository.GetByIdAsync(id);
        if (counterparty is null) return null;

        counterparty.Name    = dto.Name;
        counterparty.Type    = dto.Type;
        counterparty.Inn     = dto.Inn;
        counterparty.Address = dto.Address;

        // Обновляем контакты: новые добавляем, существующие обновляем, удалённые убираем
        var incomingIds = dto.Contacts
            .Where(c => c.Id.HasValue)
            .Select(c => c.Id!.Value)
            .ToHashSet();

        // Удаляем контакты которых нет в запросе
        counterparty.Contacts.RemoveAll(c => !incomingIds.Contains(c.Id));

        foreach (var contactDto in dto.Contacts)
        {
            if (contactDto.Id.HasValue)
            {
                // Обновляем существующий
                var existing = counterparty.Contacts.FirstOrDefault(c => c.Id == contactDto.Id.Value);
                if (existing is not null)
                {
                    existing.Name  = contactDto.Name;
                    existing.Phone = contactDto.Phone;
                    existing.Email = contactDto.Email;
                }
            }
            else
            {
                // Добавляем новый
                counterparty.Contacts.Add(new Contact
                {
                    Name  = contactDto.Name,
                    Phone = contactDto.Phone,
                    Email = contactDto.Email
                });
            }
        }

        var updated = await _counterpartyRepository.UpdateAsync(counterparty);
        return MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var counterparty = await _counterpartyRepository.GetByIdAsync(id);
        if (counterparty is null) return false;

        await _counterpartyRepository.DeleteAsync(counterparty);
        return true;
    }

    private static CounterpartyResponseDto MapToResponse(Counterparty c) => new()
    {
        Id      = c.Id,
        Name    = c.Name,
        Type    = c.Type,
        Inn     = c.Inn,
        Address = c.Address,
        Contacts = c.Contacts.Select(contact => new ContactResponseDto
        {
            Id    = contact.Id,
            Name  = contact.Name,
            Phone = contact.Phone,
            Email = contact.Email
        }).ToList()
    };
}
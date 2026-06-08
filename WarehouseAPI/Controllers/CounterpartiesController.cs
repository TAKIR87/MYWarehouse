using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WarehouseAPI.DTOs.Counterparties;
using WarehouseAPI.Models;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Контрагенты — клиенты, поставщики, компании")]
public class CounterpartiesController : ControllerBase
{
    private readonly ICounterpartyService _counterpartyService;

    public CounterpartiesController(ICounterpartyService counterpartyService)
    {
        _counterpartyService = counterpartyService;
    }

    /// <summary>Получить список всех контрагентов</summary>
    /// <remarks>
    /// Опциональный параметр `type` фильтрует по типу:
    /// `0` = Client, `1` = Supplier, `2` = Company.
    /// Без параметра возвращаются все контрагенты.
    /// </remarks>
    [HttpGet]
    [SwaggerResponse(200, "Список контрагентов", typeof(IEnumerable<CounterpartyResponseDto>))]
    public async Task<IActionResult> GetAll([FromQuery] CounterpartyType? type = null)
    {
        var counterparties = await _counterpartyService.GetAllAsync(type);
        return Ok(counterparties);
    }

    /// <summary>Получить контрагента по ID</summary>
    [HttpGet("{id:int}")]
    [SwaggerResponse(200, "Контрагент найден", typeof(CounterpartyResponseDto))]
    [SwaggerResponse(404, "Контрагент не найден")]
    public async Task<IActionResult> GetById(int id)
    {
        var counterparty = await _counterpartyService.GetByIdAsync(id);
        return counterparty is null ? NotFound() : Ok(counterparty);
    }

    /// <summary>Только клиенты (shortcut)</summary>
    [HttpGet("clients")]
    [SwaggerResponse(200, "Список клиентов", typeof(IEnumerable<CounterpartyResponseDto>))]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _counterpartyService.GetAllAsync(CounterpartyType.Client);
        return Ok(clients);
    }

    /// <summary>Только поставщики (shortcut)</summary>
    [HttpGet("suppliers")]
    [SwaggerResponse(200, "Список поставщиков", typeof(IEnumerable<CounterpartyResponseDto>))]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _counterpartyService.GetAllAsync(CounterpartyType.Supplier);
        return Ok(suppliers);
    }

    /// <summary>Только компании (shortcut)</summary>
    [HttpGet("companies")]
    [SwaggerResponse(200, "Список компаний", typeof(IEnumerable<CounterpartyResponseDto>))]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await _counterpartyService.GetAllAsync(CounterpartyType.Company);
        return Ok(companies);
    }

    /// <summary>Создать контрагента</summary>
    /// <remarks>Можно сразу передать список контактных лиц в поле `contacts`.</remarks>
    [HttpPost]
    [SwaggerResponse(201, "Контрагент успешно создан", typeof(CounterpartyResponseDto))]
    [SwaggerResponse(400, "Ошибка валидации")]
    public async Task<IActionResult> Create([FromBody] CounterpartyCreateDto dto)
    {
        var created = await _counterpartyService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Обновить контрагента</summary>
    /// <remarks>
    /// Контакты обновляются полностью по списку: контакты с `id` — обновляются,
    /// без `id` — создаются, отсутствующие — удаляются.
    /// </remarks>
    [HttpPut("{id:int}")]
    [SwaggerResponse(200, "Контрагент успешно обновлён", typeof(CounterpartyResponseDto))]
    [SwaggerResponse(400, "Ошибка валидации")]
    [SwaggerResponse(404, "Контрагент не найден")]
    public async Task<IActionResult> Update(int id, [FromBody] CounterpartyUpdateDto dto)
    {
        var updated = await _counterpartyService.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Удалить контрагента</summary>
    /// <remarks>Контакты удаляются автоматически (каскадное удаление).</remarks>
    [HttpDelete("{id:int}")]
    [SwaggerResponse(204, "Контрагент успешно удалён")]
    [SwaggerResponse(404, "Контрагент не найден")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _counterpartyService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
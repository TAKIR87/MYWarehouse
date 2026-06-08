using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WarehouseAPI.DTOs.Operations;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Складские операции — приход, продажа, перемещение, списание")]
public class OperationsController : ControllerBase
{
    private readonly IOperationService _operationService;

    public OperationsController(IOperationService operationService)
    {
        _operationService = operationService;
    }

    /// <summary>История операций с фильтрацией и пагинацией</summary>
    /// <remarks>
    /// Поддерживает фильтрацию по периоду, типу операции, контрагенту и товару.
    /// Результаты сортируются по дате — новые первыми.
    /// </remarks>
    [HttpGet("history")]
    [SwaggerResponse(200, "История операций", typeof(IEnumerable<OperationResponseDto>))]
    public async Task<IActionResult> GetHistory([FromQuery] OperationFilterDto filter)
    {
        var operations = await _operationService.GetHistoryAsync(filter);
        return Ok(operations);
    }

    /// <summary>Получить операцию по ID</summary>
    [HttpGet("{id:int}")]
    [SwaggerResponse(200, "Операция найдена", typeof(OperationResponseDto))]
    [SwaggerResponse(404, "Операция не найдена")]
    public async Task<IActionResult> GetById(int id)
    {
        var operation = await _operationService.GetByIdAsync(id);
        return operation is null ? NotFound() : Ok(operation);
    }

    /// <summary>Приход товара на склад</summary>
    /// <remarks>
    /// Увеличивает остаток каждого товара из списка items.
    /// Выполняется в транзакции — либо все позиции, либо ни одной.
    /// </remarks>
    [HttpPost("income")]
    [SwaggerResponse(201, "Приход успешно оформлен", typeof(OperationResponseDto))]
    [SwaggerResponse(400, "Ошибка валидации — пустой список товаров или неверные данные")]
    public async Task<IActionResult> Income([FromBody] IncomeCreateDto dto)
    {
        var result = await _operationService.IncomeAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Реализация товара (продажа)</summary>
    /// <remarks>
    /// Уменьшает остаток. Перед списанием проверяет наличие по всем позициям.
    /// Если хотя бы по одному товару остатка недостаточно — операция отклоняется целиком.
    /// </remarks>
    [HttpPost("sale")]
    [SwaggerResponse(201, "Продажа успешно оформлена", typeof(OperationResponseDto))]
    [SwaggerResponse(400, "Недостаточно товара на складе или ошибка валидации")]
    public async Task<IActionResult> Sale([FromBody] SaleCreateDto dto)
    {
        var result = await _operationService.SaleAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Перемещение товара</summary>
    /// <remarks>
    /// Фиксирует факт перемещения в истории. При одном складе остаток не изменяется.
    /// </remarks>
    [HttpPost("transfer")]
    [SwaggerResponse(201, "Перемещение успешно оформлено", typeof(OperationResponseDto))]
    [SwaggerResponse(400, "Ошибка валидации")]
    public async Task<IActionResult> Transfer([FromBody] TransferCreateDto dto)
    {
        var result = await _operationService.TransferAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Списание товара</summary>
    /// <remarks>
    /// Уменьшает остаток по причине порчи, брака или утери.
    /// Перед списанием проверяет наличие по всем позициям.
    /// </remarks>
    [HttpPost("writeoff")]
    [SwaggerResponse(201, "Списание успешно оформлено", typeof(OperationResponseDto))]
    [SwaggerResponse(400, "Недостаточно товара на складе или ошибка валидации")]
    public async Task<IActionResult> WriteOff([FromBody] WriteOffCreateDto dto)
    {
        var result = await _operationService.WriteOffAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
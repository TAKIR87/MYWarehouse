using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WarehouseAPI.DTOs.Analytics;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Аналитика склада")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>Топ продаваемых товаров за период</summary>
    /// <remarks>
    /// Возвращает товары отсортированные по выручке (убывание).
    /// Агрегация выполняется на стороне PostgreSQL через GROUP BY.
    ///
    /// Параметры:
    /// - `from` — начало периода (включительно)
    /// - `to` — конец периода (включительно)
    /// - `limit` — количество позиций от 1 до 100, по умолчанию 10
    /// </remarks>
    [HttpGet("top-products")]
    [SwaggerResponse(200, "Топ товаров по продажам", typeof(IEnumerable<TopProductDto>))]
    [SwaggerResponse(400, "Дата начала позже даты конца или лимит вне диапазона 1–100")]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int limit = 10)
    {
        if (from > to)
            return BadRequest(new { message = "Дата начала не может быть позже даты конца" });

        if (limit is < 1 or > 100)
            return BadRequest(new { message = "Лимит должен быть от 1 до 100" });

        var result = await _analyticsService.GetTopProductsAsync(from, to, limit);
        return Ok(result);
    }

    /// <summary>Обороты за период</summary>
    /// <remarks>
    /// Возвращает суммы приходов, продаж и списаний за указанный период,
    /// а также разбивку по каждому дню.
    ///
    /// Поле `profit` = saleAmount − incomeAmount (грубая прибыль без учёта списаний).
    /// </remarks>
    [HttpGet("turnover")]
    [SwaggerResponse(200, "Обороты за период", typeof(TurnoverDto))]
    [SwaggerResponse(400, "Дата начала позже даты конца")]
    public async Task<IActionResult> GetTurnover(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (from > to)
            return BadRequest(new { message = "Дата начала не может быть позже даты конца" });

        var result = await _analyticsService.GetTurnoverAsync(from, to);
        return Ok(result);
    }

    /// <summary>Товары с остатком ниже минимального порога</summary>
    /// <remarks>
    /// Возвращает список товаров отсортированных по остатку (возрастание) —
    /// самые критичные первыми. Поле `deficit` показывает сколько единиц не хватает.
    ///
    /// Параметр `minQuantity` — порог, по умолчанию 5.
    /// </remarks>
    [HttpGet("low-stock")]
    [SwaggerResponse(200, "Товары с низким остатком", typeof(IEnumerable<LowStockDto>))]
    [SwaggerResponse(400, "minQuantity не может быть отрицательным")]
    public async Task<IActionResult> GetLowStock([FromQuery] decimal minQuantity = 5)
    {
        if (minQuantity < 0)
            return BadRequest(new { message = "Минимальное количество не может быть отрицательным" });

        var result = await _analyticsService.GetLowStockAsync(minQuantity);
        return Ok(result);
    }
}
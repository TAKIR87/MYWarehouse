using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    // GET api/analytics/top-products?from=2024-01-01&to=2024-12-31&limit=10
    [HttpGet("top-products")]
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

    // GET api/analytics/turnover?from=2024-01-01&to=2024-12-31
    [HttpGet("turnover")]
    public async Task<IActionResult> GetTurnover(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        if (from > to)
            return BadRequest(new { message = "Дата начала не может быть позже даты конца" });

        var result = await _analyticsService.GetTurnoverAsync(from, to);
        return Ok(result);
    }

    // GET api/analytics/low-stock?minQuantity=5
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock([FromQuery] decimal minQuantity = 5)
    {
        if (minQuantity < 0)
            return BadRequest(new { message = "Минимальное количество не может быть отрицательным" });

        var result = await _analyticsService.GetLowStockAsync(minQuantity);
        return Ok(result);
    }
}
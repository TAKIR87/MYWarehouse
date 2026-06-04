using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WarehouseAPI.DTOs.Stock;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Остатки на складе")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    /// <summary>Получить остатки по всем товарам</summary>
    /// <remarks>
    /// Возвращает текущие складские остатки с наименованием товара,
    /// артикулом, единицей измерения и общей стоимостью остатка (quantity × price).
    /// </remarks>
    [HttpGet]
    [SwaggerResponse(200, "Список остатков", typeof(IEnumerable<StockResponseDto>))]
    public async Task<IActionResult> GetAll()
    {
        var stocks = await _stockService.GetAllAsync();
        return Ok(stocks);
    }

    /// <summary>Получить остаток по ID товара</summary>
    [HttpGet("{productId:int}")]
    [SwaggerResponse(200, "Остаток найден", typeof(StockResponseDto))]
    [SwaggerResponse(404, "Товар не найден")]
    public async Task<IActionResult> GetByProductId(int productId)
    {
        var stock = await _stockService.GetByProductIdAsync(productId);
        return stock is null ? NotFound() : Ok(stock);
    }
}
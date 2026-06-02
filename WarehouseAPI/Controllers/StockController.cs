using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
	private readonly IStockService _stockService;

	public StockController(IStockService stockService)
	{
		_stockService = stockService;
	}

	// GET api/stock
	[HttpGet]
	public async Task<IActionResult> GetAll()
	{
		var stocks = await _stockService.GetAllAsync();
		return Ok(stocks);
	}

	// GET api/stock/5
	[HttpGet("{productId:int}")]
	public async Task<IActionResult> GetByProductId(int productId)
	{
		var stock = await _stockService.GetByProductIdAsync(productId);
		return stock is null ? NotFound() : Ok(stock);
	}
}
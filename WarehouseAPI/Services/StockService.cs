using WarehouseAPI.DTOs.Stock;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;

    public StockService(IStockRepository stockRepository)
    {
        _stockRepository = stockRepository;
    }

    public async Task<IEnumerable<StockResponseDto>> GetAllAsync()
    {
        var stocks = await _stockRepository.GetAllAsync();
        return stocks.Select(MapToResponse);
    }

    public async Task<StockResponseDto?> GetByProductIdAsync(int productId)
    {
        var stock = await _stockRepository.GetByProductIdAsync(productId);
        return stock is null ? null : MapToResponse(stock);
    }

    private static StockResponseDto MapToResponse(Stock s) => new()
    {
        ProductId = s.ProductId,
        ProductName = s.Product.Name,
        ProductArticle = s.Product.Article,
        Unit = s.Product.Unit,
        Quantity = s.Quantity,
        Price = s.Product.Price,
        UpdatedAt = s.UpdatedAt
    };
}
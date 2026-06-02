using WarehouseAPI.DTOs.Analytics;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IProductRepository _productRepository;

    public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        IProductRepository productRepository)
    {
        _analyticsRepository = analyticsRepository;
        _productRepository   = productRepository;
    }

    // ─── ТОП ПРОДАВАЕМЫХ ТОВАРОВ ───────────────────────────────────────────────
    public async Task<IEnumerable<TopProductDto>> GetTopProductsAsync(
        DateTime from, DateTime to, int limit = 10)
    {
        var rows = (await _analyticsRepository.GetTopProductsAsync(from, to, limit)).ToList();

        if (!rows.Any()) return Enumerable.Empty<TopProductDto>();

        // Подтягиваем названия товаров одним запросом
        var productIds = rows.Select(r => r.ProductId).ToList();
        var products   = (await _productRepository.GetAllAsync())
            .Where(p => productIds.Contains(p.Id))
            .ToDictionary(p => p.Id);

        return rows.Select(r =>
        {
            products.TryGetValue(r.ProductId, out var product);
            return new TopProductDto
            {
                ProductId      = r.ProductId,
                ProductName    = product?.Name    ?? $"Товар #{r.ProductId}",
                ProductArticle = product?.Article ?? string.Empty,
                Unit           = product?.Unit    ?? string.Empty,
                TotalQuantity  = r.TotalQuantity,
                TotalAmount    = r.TotalAmount,
                OperationsCount = r.Count
            };
        });
    }

    // ─── ОБОРОТЫ ЗА ПЕРИОД ─────────────────────────────────────────────────────
    public async Task<TurnoverDto> GetTurnoverAsync(DateTime from, DateTime to)
    {
        var operations = await _analyticsRepository.GetOperationsForTurnoverAsync(from, to);

        var turnover = new TurnoverDto
        {
            From          = from,
            To            = to,
            IncomeAmount  = SumByType(operations, OperationType.Income),
            SaleAmount    = SumByType(operations, OperationType.Sale),
            WriteOffAmount = SumByType(operations, OperationType.WriteOff)
        };

        // Разбивка по дням — группируем по дате
        turnover.ByDay = operations
            .GroupBy(o => o.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TurnoverByDayDto
            {
                Date         = g.Key,
                IncomeAmount = g.Where(o => o.Type == OperationType.Income)
                                .SelectMany(o => o.Items)
                                .Sum(i => i.Quantity * i.Price),
                SaleAmount   = g.Where(o => o.Type == OperationType.Sale)
                                .SelectMany(o => o.Items)
                                .Sum(i => i.Quantity * i.Price)
            })
            .ToList();

        return turnover;
    }

    // ─── ОСТАТКИ НИЖЕ МИНИМУМА ─────────────────────────────────────────────────
    public async Task<IEnumerable<LowStockDto>> GetLowStockAsync(decimal minQuantity)
    {
        var stocks = await _analyticsRepository.GetAllStocksWithProductsAsync();

        return stocks
            .Where(s => s.Quantity < minQuantity)
            .OrderBy(s => s.Quantity)   // сначала самые критичные
            .Select(s => new LowStockDto
            {
                ProductId       = s.ProductId,
                ProductName     = s.Product.Name,
                ProductArticle  = s.Product.Article,
                Unit            = s.Product.Unit,
                CurrentQuantity = s.Quantity,
                MinQuantity     = minQuantity
            });
    }

    // ─── ХЕЛПЕР ────────────────────────────────────────────────────────────────
    private static decimal SumByType(IEnumerable<Operation> operations, OperationType type)
    {
        return operations
            .Where(o => o.Type == type)
            .SelectMany(o => o.Items)
            .Sum(i => i.Quantity * i.Price);
    }
}
using WarehouseAPI.DTOs.Products;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger            = logger;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllAsync()
    {
        _logger.LogInformation("Запрос списка всех товаров");
        var products = await _productRepository.GetAllAsync();
        var list = products.ToList();
        _logger.LogInformation("Возвращено {Count} товаров", list.Count);
        return list.Select(MapToResponse);
    }

    public async Task<ProductResponseDto?> GetByIdAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
            _logger.LogInformation("Товар ID={ProductId} не найден", id);
        return product is null ? null : MapToResponse(product);
    }

    public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
    {
        _logger.LogInformation(
            "Создание товара: Name={Name}, Article={Article}",
            dto.Name, dto.Article);

        if (await _productRepository.ArticleExistsAsync(dto.Article))
        {
            _logger.LogWarning(
                "Попытка создать товар с дублирующимся артикулом '{Article}'",
                dto.Article);
            throw new InvalidOperationException(
                $"Товар с артикулом '{dto.Article}' уже существует");
        }

        var product = new Product
        {
            Name      = dto.Name,
            Article   = dto.Article,
            Unit      = dto.Unit,
            Price     = dto.Price,
            CreatedAt = DateTime.UtcNow,
            Stock     = new Stock { Quantity = 0, UpdatedAt = DateTime.UtcNow }
        };

        var created = await _productRepository.CreateAsync(product);

        _logger.LogInformation(
            "Товар создан: ID={ProductId}, Name={Name}, Article={Article}",
            created.Id, created.Name, created.Article);

        return MapToResponse(created);
    }

    public async Task<ProductResponseDto?> UpdateAsync(int id, ProductUpdateDto dto)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            _logger.LogInformation("Обновление: товар ID={ProductId} не найден", id);
            return null;
        }

        if (product.Article != dto.Article &&
            await _productRepository.ArticleExistsAsync(dto.Article, excludeId: id))
        {
            _logger.LogWarning(
                "Обновление товара ID={ProductId}: артикул '{Article}' уже занят",
                id, dto.Article);
            throw new InvalidOperationException(
                $"Товар с артикулом '{dto.Article}' уже существует");
        }

        product.Name    = dto.Name;
        product.Article = dto.Article;
        product.Unit    = dto.Unit;
        product.Price   = dto.Price;

        var updated = await _productRepository.UpdateAsync(product);

        _logger.LogInformation(
            "Товар ID={ProductId} обновлён: Name={Name}, Article={Article}, Price={Price}",
            updated.Id, updated.Name, updated.Article, updated.Price);

        return MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            _logger.LogInformation("Удаление: товар ID={ProductId} не найден", id);
            return false;
        }

        if (product.Stock is not null && product.Stock.Quantity > 0)
        {
            _logger.LogWarning(
                "Попытка удалить товар '{Name}' (ID={ProductId}) с ненулевым остатком {Quantity} {Unit}",
                product.Name, id, product.Stock.Quantity, product.Unit);
            throw new InvalidOperationException(
                $"Нельзя удалить товар '{product.Name}': остаток на складе {product.Stock.Quantity} {product.Unit}");
        }

        await _productRepository.DeleteAsync(product);

        _logger.LogInformation(
            "Товар ID={ProductId} Name={Name} удалён",
            id, product.Name);

        return true;
    }

    private static ProductResponseDto MapToResponse(Product p) => new()
    {
        Id           = p.Id,
        Name         = p.Name,
        Article      = p.Article,
        Unit         = p.Unit,
        Price        = p.Price,
        CurrentStock = p.Stock?.Quantity ?? 0,
        CreatedAt    = p.CreatedAt
    };
}
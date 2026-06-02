using WarehouseAPI.DTOs.Products;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Services;

public class ProductService : IProductService
{
	private readonly IProductRepository _productRepository;

	public ProductService(IProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public async Task<IEnumerable<ProductResponseDto>> GetAllAsync()
	{
		var products = await _productRepository.GetAllAsync();
		return products.Select(MapToResponse);
	}

	public async Task<ProductResponseDto?> GetByIdAsync(int id)
	{
		var product = await _productRepository.GetByIdAsync(id);
		return product is null ? null : MapToResponse(product);
	}

	public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
	{
		// Проверяем уникальность артикула
		if (await _productRepository.ArticleExistsAsync(dto.Article))
			throw new InvalidOperationException($"Товар с артикулом '{dto.Article}' уже существует");

		var product = new Product
		{
			Name = dto.Name,
			Article = dto.Article,
			Unit = dto.Unit,
			Price = dto.Price,
			CreatedAt = DateTime.UtcNow,
			// Сразу создаём запись остатка с нулём
			Stock = new Stock { Quantity = 0, UpdatedAt = DateTime.UtcNow }
		};

		var created = await _productRepository.CreateAsync(product);
		return MapToResponse(created);
	}

	public async Task<ProductResponseDto?> UpdateAsync(int id, ProductUpdateDto dto)
	{
		var product = await _productRepository.GetByIdAsync(id);
		if (product is null) return null;

		// Проверяем артикул только если он изменился
		if (product.Article != dto.Article &&
			await _productRepository.ArticleExistsAsync(dto.Article, excludeId: id))
			throw new InvalidOperationException($"Товар с артикулом '{dto.Article}' уже существует");

		product.Name = dto.Name;
		product.Article = dto.Article;
		product.Unit = dto.Unit;
		product.Price = dto.Price;

		var updated = await _productRepository.UpdateAsync(product);
		return MapToResponse(updated);
	}

	public async Task<bool> DeleteAsync(int id)
	{
		var product = await _productRepository.GetByIdAsync(id);
		if (product is null) return false;

		// Не даём удалить товар если есть ненулевой остаток
		if (product.Stock is not null && product.Stock.Quantity > 0)
			throw new InvalidOperationException(
				$"Нельзя удалить товар '{product.Name}': остаток на складе {product.Stock.Quantity} {product.Unit}");

		await _productRepository.DeleteAsync(product);
		return true;
	}

	// Маппинг модели → DTO, в одном месте
	private static ProductResponseDto MapToResponse(Product p) => new()
	{
		Id = p.Id,
		Name = p.Name,
		Article = p.Article,
		Unit = p.Unit,
		Price = p.Price,
		CurrentStock = p.Stock?.Quantity ?? 0,
		CreatedAt = p.CreatedAt
	};
}
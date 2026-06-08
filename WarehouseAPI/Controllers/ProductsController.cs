using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using WarehouseAPI.DTOs.Products;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[SwaggerTag("Управление товарами")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Получить список всех товаров</summary>
    /// <remarks>Возвращает все товары с текущими остатками на складе.</remarks>
    [HttpGet]
    [SwaggerResponse(200, "Список товаров успешно получен", typeof(IEnumerable<ProductResponseDto>))]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllAsync();
        return Ok(products);
    }

    /// <summary>Получить товар по ID</summary>
    [HttpGet("{id:int}")]
    [SwaggerResponse(200, "Товар найден", typeof(ProductResponseDto))]
    [SwaggerResponse(404, "Товар не найден")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Создать новый товар</summary>
    /// <remarks>
    /// При создании автоматически создаётся запись остатка с нулевым количеством.
    /// Артикул должен быть уникальным.
    /// </remarks>
    [HttpPost]
    [SwaggerResponse(201, "Товар успешно создан", typeof(ProductResponseDto))]
    [SwaggerResponse(400, "Ошибка валидации или артикул уже занят")]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        var created = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Обновить товар</summary>
    [HttpPut("{id:int}")]
    [SwaggerResponse(200, "Товар успешно обновлён", typeof(ProductResponseDto))]
    [SwaggerResponse(400, "Ошибка валидации или артикул уже занят")]
    [SwaggerResponse(404, "Товар не найден")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateDto dto)
    {
        var updated = await _productService.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Удалить товар</summary>
    /// <remarks>Нельзя удалить товар если его остаток на складе больше нуля.</remarks>
    [HttpDelete("{id:int}")]
    [SwaggerResponse(204, "Товар успешно удалён")]
    [SwaggerResponse(400, "Невозможно удалить — остаток больше нуля")]
    [SwaggerResponse(404, "Товар не найден")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _productService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
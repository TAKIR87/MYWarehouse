using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Data;
using WarehouseAPI.DTOs.Operations;
using WarehouseAPI.Models;
using WarehouseAPI.Repositories.Interfaces;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Services;

public class OperationService : IOperationService
{
    private readonly IOperationRepository _operationRepository;
    private readonly AppDbContext _context;  // нужен для транзакций

    public OperationService(IOperationRepository operationRepository, AppDbContext context)
    {
        _operationRepository = operationRepository;
        _context = context;
    }

    // ─── ПРИХОД ────────────────────────────────────────────────────────────────
    public async Task<OperationResponseDto> IncomeAsync(IncomeCreateDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var operation = new Operation
            {
                Type = OperationType.Income,
                Date = DateTime.UtcNow,
                Comment = dto.Comment,
                CounterpartyId = dto.CounterpartyId,
                Items = dto.Items.Select(i => new OperationItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            await _operationRepository.CreateAsync(operation);

            // Увеличиваем остаток по каждому товару
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException(
                        $"Остаток для товара ID={item.ProductId} не найден");

                stock.Quantity += item.Quantity;
                await _operationRepository.UpdateStockAsync(stock);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToResponse(await _operationRepository.GetByIdAsync(operation.Id)
                ?? throw new Exception("Ошибка при получении созданной операции"));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ─── РЕАЛИЗАЦИЯ (ПРОДАЖА) ──────────────────────────────────────────────────
    public async Task<OperationResponseDto> SaleAsync(SaleCreateDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Сначала проверяем остатки по ВСЕМ товарам до любых изменений
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException(
                        $"Остаток для товара ID={item.ProductId} не найден");

                if (stock.Quantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Недостаточно товара ID={item.ProductId}: " +
                        $"на складе {stock.Quantity}, запрошено {item.Quantity}");
            }

            var operation = new Operation
            {
                Type = OperationType.Sale,
                Date = DateTime.UtcNow,
                Comment = dto.Comment,
                CounterpartyId = dto.CounterpartyId,
                Items = dto.Items.Select(i => new OperationItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            await _operationRepository.CreateAsync(operation);

            // Уменьшаем остатки
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)!;
                stock.Quantity -= item.Quantity;
                await _operationRepository.UpdateStockAsync(stock);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToResponse(await _operationRepository.GetByIdAsync(operation.Id)
                ?? throw new Exception("Ошибка при получении созданной операции"));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ─── ПЕРЕМЕЩЕНИЕ ───────────────────────────────────────────────────────────
    // В текущей схеме один склад — перемещение меняет только Comment/тип.
    // Если появятся склады — здесь будет: списать с одного, добавить на другой.
    public async Task<OperationResponseDto> TransferAsync(TransferCreateDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Проверяем наличие товаров
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException(
                        $"Остаток для товара ID={item.ProductId} не найден");

                if (stock.Quantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Недостаточно товара ID={item.ProductId}: " +
                        $"на складе {stock.Quantity}, запрошено {item.Quantity}");
            }

            var operation = new Operation
            {
                Type = OperationType.Transfer,
                Date = DateTime.UtcNow,
                Comment = dto.Comment,
                Items = dto.Items.Select(i => new OperationItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            await _operationRepository.CreateAsync(operation);

            // Остатки не меняются (один склад) — только фиксируем факт перемещения
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToResponse(await _operationRepository.GetByIdAsync(operation.Id)
                ?? throw new Exception("Ошибка при получении созданной операции"));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ─── СПИСАНИЕ ──────────────────────────────────────────────────────────────
    public async Task<OperationResponseDto> WriteOffAsync(WriteOffCreateDto dto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Проверяем остатки
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException(
                        $"Остаток для товара ID={item.ProductId} не найден");

                if (stock.Quantity < item.Quantity)
                    throw new InvalidOperationException(
                        $"Недостаточно товара ID={item.ProductId}: " +
                        $"на складе {stock.Quantity}, запрошено {item.Quantity}");
            }

            var operation = new Operation
            {
                Type = OperationType.WriteOff,
                Date = DateTime.UtcNow,
                Comment = dto.Comment,  // причина списания
                Items = dto.Items.Select(i => new OperationItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            await _operationRepository.CreateAsync(operation);

            // Уменьшаем остатки (аналогично продаже, но другой тип операции)
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)!;
                stock.Quantity -= item.Quantity;
                await _operationRepository.UpdateStockAsync(stock);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToResponse(await _operationRepository.GetByIdAsync(operation.Id)
                ?? throw new Exception("Ошибка при получении созданной операции"));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ─── ИСТОРИЯ И ПОЛУЧЕНИЕ ───────────────────────────────────────────────────
    public async Task<IEnumerable<OperationResponseDto>> GetHistoryAsync(OperationFilterDto filter)
    {
        var operations = await _operationRepository.GetHistoryAsync(
            filter.From,
            filter.To,
            filter.Type,
            filter.CounterpartyId,
            filter.ProductId,
            filter.Page,
            filter.PageSize);

        return operations.Select(MapToResponse);
    }

    public async Task<OperationResponseDto?> GetByIdAsync(int id)
    {
        var operation = await _operationRepository.GetByIdAsync(id);
        return operation is null ? null : MapToResponse(operation);
    }

    // ─── МАППИНГ ───────────────────────────────────────────────────────────────
    private static OperationResponseDto MapToResponse(Operation o) => new()
    {
        Id = o.Id,
        Type = o.Type,
        Date = o.Date,
        Comment = o.Comment,
        CounterpartyId = o.CounterpartyId,
        CounterpartyName = o.Counterparty?.Name,
        Items = o.Items.Select(i => new OperationItemResponseDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            ProductArticle = i.Product?.Article ?? string.Empty,
            Quantity = i.Quantity,
            Price = i.Price
        }).ToList()
    };
}
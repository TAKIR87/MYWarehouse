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
    private readonly AppDbContext _context;
    private readonly ILogger<OperationService> _logger;

    public OperationService(
        IOperationRepository operationRepository,
        AppDbContext context,
        ILogger<OperationService> logger)
    {
        _operationRepository = operationRepository;
        _context             = context;
        _logger              = logger;
    }

    // ─── ПРИХОД ────────────────────────────────────────────────────────────────
    public async Task<OperationResponseDto> IncomeAsync(IncomeCreateDto dto)
    {
        _logger.LogInformation(
            "Начало операции Приход: {ItemCount} позиций, контрагент ID={CounterpartyId}",
            dto.Items.Count, dto.CounterpartyId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var operation = new Operation
            {
                Type           = OperationType.Income,
                Date           = DateTime.UtcNow,
                Comment        = dto.Comment,
                CounterpartyId = dto.CounterpartyId,
                Items          = dto.Items.Select(i => new OperationItem
                {
                    ProductId = i.ProductId,
                    Quantity  = i.Quantity,
                    Price     = i.Price
                }).ToList()
            };

            await _operationRepository.CreateAsync(operation);

            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException(
                        $"Остаток для товара ID={item.ProductId} не найден");

                var oldQty = stock.Quantity;
                stock.Quantity += item.Quantity;
                await _operationRepository.UpdateStockAsync(stock);

                _logger.LogInformation(
                    "Приход товара ID={ProductId}: {OldQty} → {NewQty} ({Unit})",
                    item.ProductId, oldQty, stock.Quantity, item.Quantity);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Операция Приход ID={OperationId} успешно завершена",
                operation.Id);

            return MapToResponse(await _operationRepository.GetByIdAsync(operation.Id)
                ?? throw new Exception("Ошибка при получении созданной операции"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogCritical(ex,
                "ОТКАТ транзакции Приход: {Message}", ex.Message);
            throw;
        }
    }

    // ─── РЕАЛИЗАЦИЯ (ПРОДАЖА) ──────────────────────────────────────────────────
    public async Task<OperationResponseDto> SaleAsync(SaleCreateDto dto)
    {
        _logger.LogInformation(
            "Начало операции Реализация: {ItemCount} позиций, контрагент ID={CounterpartyId}",
            dto.Items.Count, dto.CounterpartyId);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Проверяем остатки по ВСЕМ товарам до любых изменений
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException(
                        $"Остаток для товара ID={item.ProductId} не найден");

                if (stock.Quantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Реализация отклонена: недостаточно товара ID={ProductId} — " +
                        "на складе {Available}, запрошено {Requested}",
                        item.ProductId, stock.Quantity, item.Quantity);

                    throw new InvalidOperationException(
                        $"Недостаточно товара ID={item.ProductId}: " +
                        $"на складе {stock.Quantity}, запрошено {item.Quantity}");
                }
            }

            var operation = new Operation
            {
                Type           = OperationType.Sale,
                Date           = DateTime.UtcNow,
                Comment        = dto.Comment,
                CounterpartyId = dto.CounterpartyId,
                Items          = dto.Items.Select(i => new OperationItem
                {
                    ProductId = i.ProductId,
                    Quantity  = i.Quantity,
                    Price     = i.Price
                }).ToList()
            };

            await _operationRepository.CreateAsync(operation);

            foreach (var item in dto.Items)
            {
                var stock = (await _operationRepository.GetStockAsync(item.ProductId))!;
                var oldQty = stock.Quantity;
                stock.Quantity -= item.Quantity;
                await _operationRepository.UpdateStockAsync(stock);

                _logger.LogInformation(
                    "Реализация товара ID={ProductId}: {OldQty} → {NewQty}",
                    item.ProductId, oldQty, stock.Quantity);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Операция Реализация ID={OperationId} успешно завершена",
                operation.Id);

            return MapToResponse(await _operationRepository.GetByIdAsync(operation.Id)
                ?? throw new Exception("Ошибка при получении созданной операции"));
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogCritical(ex,
                "ОТКАТ транзакции Реализация: {Message}", ex.Message);
            throw;
        }
    }

    // ─── ПЕРЕМЕЩЕНИЕ ───────────────────────────────────────────────────────────
    public async Task<OperationResponseDto> TransferAsync(TransferCreateDto dto)
    {
        _logger.LogInformation(
            "Начало операции Перемещение: {ItemCount} позиций",
            dto.Items.Count);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException(
                        $"Остаток для товара ID={item.ProductId} не найден");

                if (stock.Quantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Перемещение отклонено: недостаточно товара ID={ProductId}",
                        item.ProductId);

                    throw new InvalidOperationException(
                        $"Недостаточно товара ID={item.ProductId}: " +
                        $"на складе {stock.Quantity}, запрошено {item.Quantity}");
                }
            }

            var operation = new Operation
            {
                Type    = OperationType.Transfer,
                Date    = DateTime.UtcNow,
                Comment = dto.Comment,
                Items   = dto.Items.Select(i => new OperationItem
                {
                    ProductId = i.ProductId,
                    Quantity  = i.Quantity,
                    Price     = i.Price
                }).ToList()
            };

            await _operationRepository.CreateAsync(operation);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Операция Перемещение ID={OperationId} успешно зафиксирована",
                operation.Id);

            return MapToResponse(await _operationRepository.GetByIdAsync(operation.Id)
                ?? throw new Exception("Ошибка при получении созданной операции"));
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogCritical(ex,
                "ОТКАТ транзакции Перемещение: {Message}", ex.Message);
            throw;
        }
    }

    // ─── СПИСАНИЕ ──────────────────────────────────────────────────────────────
    public async Task<OperationResponseDto> WriteOffAsync(WriteOffCreateDto dto)
    {
        _logger.LogInformation(
            "Начало операции Списание: {ItemCount} позиций. Причина: {Comment}",
            dto.Items.Count, dto.Comment ?? "не указана");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in dto.Items)
            {
                var stock = await _operationRepository.GetStockAsync(item.ProductId)
                    ?? throw new InvalidOperationException(
                        $"Остаток для товара ID={item.ProductId} не найден");

                if (stock.Quantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Списание отклонено: недостаточно товара ID={ProductId} — " +
                        "на складе {Available}, запрошено {Requested}",
                        item.ProductId, stock.Quantity, item.Quantity);

                    throw new InvalidOperationException(
                        $"Недостаточно товара ID={item.ProductId}: " +
                        $"на складе {stock.Quantity}, запрошено {item.Quantity}");
                }
            }

            var operation = new Operation
            {
                Type    = OperationType.WriteOff,
                Date    = DateTime.UtcNow,
                Comment = dto.Comment,
                Items   = dto.Items.Select(i => new OperationItem
                {
                    ProductId = i.ProductId,
                    Quantity  = i.Quantity,
                    Price     = i.Price
                }).ToList()
            };

            await _operationRepository.CreateAsync(operation);

            foreach (var item in dto.Items)
            {
                var stock = (await _operationRepository.GetStockAsync(item.ProductId))!;
                var oldQty = stock.Quantity;
                stock.Quantity -= item.Quantity;
                await _operationRepository.UpdateStockAsync(stock);

                _logger.LogInformation(
                    "Списание товара ID={ProductId}: {OldQty} → {NewQty}",
                    item.ProductId, oldQty, stock.Quantity);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Операция Списание ID={OperationId} успешно завершена",
                operation.Id);

            return MapToResponse(await _operationRepository.GetByIdAsync(operation.Id)
                ?? throw new Exception("Ошибка при получении созданной операции"));
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogCritical(ex,
                "ОТКАТ транзакции Списание: {Message}", ex.Message);
            throw;
        }
    }

    // ─── ИСТОРИЯ И ПОЛУЧЕНИЕ ───────────────────────────────────────────────────
    public async Task<IEnumerable<OperationResponseDto>> GetHistoryAsync(OperationFilterDto filter)
    {
        _logger.LogInformation(
            "Запрос истории операций: страница {Page}, размер {PageSize}, тип {Type}",
            filter.Page, filter.PageSize, filter.Type?.ToString() ?? "все");

        var operations = await _operationRepository.GetHistoryAsync(
            filter.From, filter.To, filter.Type,
            filter.CounterpartyId, filter.ProductId,
            filter.Page, filter.PageSize);

        return operations.Select(MapToResponse);
    }

    public async Task<OperationResponseDto?> GetByIdAsync(int id)
    {
        var operation = await _operationRepository.GetByIdAsync(id);
        if (operation is null)
            _logger.LogInformation("Операция ID={OperationId} не найдена", id);

        return operation is null ? null : MapToResponse(operation);
    }

    // ─── МАППИНГ ───────────────────────────────────────────────────────────────
    private static OperationResponseDto MapToResponse(Operation o) => new()
    {
        Id               = o.Id,
        Type             = o.Type,
        Date             = o.Date,
        Comment          = o.Comment,
        CounterpartyId   = o.CounterpartyId,
        CounterpartyName = o.Counterparty?.Name,
        Items            = o.Items.Select(i => new OperationItemResponseDto
        {
            ProductId      = i.ProductId,
            ProductName    = i.Product?.Name    ?? string.Empty,
            ProductArticle = i.Product?.Article ?? string.Empty,
            Quantity       = i.Quantity,
            Price          = i.Price
        }).ToList()
    };
}
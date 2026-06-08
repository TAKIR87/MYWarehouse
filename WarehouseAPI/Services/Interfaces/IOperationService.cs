using WarehouseAPI.DTOs.Operations;

namespace WarehouseAPI.Services.Interfaces;

public interface IOperationService
{
    Task<OperationResponseDto> IncomeAsync(IncomeCreateDto dto);
    Task<OperationResponseDto> SaleAsync(SaleCreateDto dto);
    Task<OperationResponseDto> TransferAsync(TransferCreateDto dto);
    Task<OperationResponseDto> WriteOffAsync(WriteOffCreateDto dto);
    Task<IEnumerable<OperationResponseDto>> GetHistoryAsync(OperationFilterDto filter);
    Task<OperationResponseDto?> GetByIdAsync(int id);
}
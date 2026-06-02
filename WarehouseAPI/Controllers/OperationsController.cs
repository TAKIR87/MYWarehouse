using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.DTOs.Operations;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private readonly IOperationService _operationService;

    public OperationsController(IOperationService operationService)
    {
        _operationService = operationService;
    }

    // GET api/operations/history?from=2024-01-01&to=2024-12-31&type=Sale&page=1&pageSize=20
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] OperationFilterDto filter)
    {
        var operations = await _operationService.GetHistoryAsync(filter);
        return Ok(operations);
    }

    // GET api/operations/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var operation = await _operationService.GetByIdAsync(id);
        return operation is null ? NotFound() : Ok(operation);
    }

    // POST api/operations/income
    [HttpPost("income")]
    public async Task<IActionResult> Income([FromBody] IncomeCreateDto dto)
    {
        var result = await _operationService.IncomeAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // POST api/operations/sale
    [HttpPost("sale")]
    public async Task<IActionResult> Sale([FromBody] SaleCreateDto dto)
    {
        var result = await _operationService.SaleAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // POST api/operations/transfer
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferCreateDto dto)
    {
        var result = await _operationService.TransferAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // POST api/operations/writeoff
    [HttpPost("writeoff")]
    public async Task<IActionResult> WriteOff([FromBody] WriteOffCreateDto dto)
    {
        var result = await _operationService.WriteOffAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
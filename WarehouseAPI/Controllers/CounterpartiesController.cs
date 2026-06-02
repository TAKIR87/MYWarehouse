using Microsoft.AspNetCore.Mvc;
using WarehouseAPI.DTOs.Counterparties;
using WarehouseAPI.Models;
using WarehouseAPI.Services.Interfaces;

namespace WarehouseAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CounterpartiesController : ControllerBase
{
    private readonly ICounterpartyService _counterpartyService;

    public CounterpartiesController(ICounterpartyService counterpartyService)
    {
        _counterpartyService = counterpartyService;
    }

    // GET api/counterparties
    // GET api/counterparties?type=Client
    // GET api/counterparties?type=Supplier
    // GET api/counterparties?type=Company
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CounterpartyType? type = null)
    {
        var counterparties = await _counterpartyService.GetAllAsync(type);
        return Ok(counterparties);
    }

    // GET api/counterparties/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var counterparty = await _counterpartyService.GetByIdAsync(id);
        return counterparty is null ? NotFound() : Ok(counterparty);
    }

    // GET api/counterparties/clients  — удобный шорткат для фронтенда
    [HttpGet("clients")]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _counterpartyService.GetAllAsync(CounterpartyType.Client);
        return Ok(clients);
    }

    // GET api/counterparties/suppliers
    [HttpGet("suppliers")]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _counterpartyService.GetAllAsync(CounterpartyType.Supplier);
        return Ok(suppliers);
    }

    // GET api/counterparties/companies
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        var companies = await _counterpartyService.GetAllAsync(CounterpartyType.Company);
        return Ok(companies);
    }

    // POST api/counterparties
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CounterpartyCreateDto dto)
    {
        var created = await _counterpartyService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT api/counterparties/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] CounterpartyUpdateDto dto)
    {
        var updated = await _counterpartyService.UpdateAsync(id, dto);
        return updated is null ? NotFound() : Ok(updated);
    }

    // DELETE api/counterparties/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _counterpartyService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
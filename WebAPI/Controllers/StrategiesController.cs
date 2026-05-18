using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Manages strategies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StrategiesController : ControllerBase
{
    private readonly IStrategyService _strategyService;

    public StrategiesController(IStrategyService strategyService)
    {
        _strategyService = strategyService;
    }

    /// <summary>
    /// Returns all strategies.
    /// </summary>
    /// <response code="200">List of strategies.</response>
    [HttpGet]
    public async Task<ActionResult<List<StrategyDto>>> GetAll()
    {
        var result = await _strategyService.GetAllAsync();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new strategy.
    /// </summary>
    /// <param name="dto">Strategy payload.</param>
    /// <response code="200">Strategy created successfully.</response>
    [HttpPost]
    public async Task<ActionResult<StrategyDto>> Create([FromBody] StrategyDto dto)
    {
        var result = await _strategyService.CreateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing strategy.
    /// </summary>
    /// <param name="id">Strategy identifier.</param>
    /// <param name="dto">Strategy payload.</param>
    /// <response code="200">Strategy updated successfully.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StrategyDto>> Update(Guid id, [FromBody] StrategyDto dto)
    {
        dto.Id = id;
        var result = await _strategyService.UpdateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a strategy by identifier.
    /// </summary>
    /// <param name="id">Strategy identifier.</param>
    /// <response code="204">Strategy deleted successfully.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _strategyService.RemoveAsync(id);
        return NoContent();
    }
}

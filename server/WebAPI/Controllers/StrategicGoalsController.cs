using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Manages strategic goals.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StrategicGoalsController : ControllerBase
{
    private readonly IStrategicGoalService _strategicGoalService;

    public StrategicGoalsController(IStrategicGoalService strategicGoalService)
    {
        _strategicGoalService = strategicGoalService;
    }

    /// <summary>
    /// Returns all strategic goals.
    /// </summary>
    /// <response code="200">List of strategic goals.</response>
    [HttpGet]
    public async Task<ActionResult<List<StrategicGoalDto>>> GetAll()
    {
        var result = await _strategicGoalService.GetAllAsync();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new strategic goal.
    /// </summary>
    /// <param name="dto">Strategic goal payload.</param>
    /// <response code="200">Strategic goal created successfully.</response>
    [HttpPost]
    public async Task<ActionResult<StrategicGoalDto>> Create([FromBody] StrategicGoalDto dto)
    {
        var result = await _strategicGoalService.CreateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing strategic goal.
    /// </summary>
    /// <param name="id">Strategic goal identifier.</param>
    /// <param name="dto">Strategic goal payload.</param>
    /// <response code="200">Strategic goal updated successfully.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StrategicGoalDto>> Update(Guid id, [FromBody] StrategicGoalDto dto)
    {
        dto.Id = id;
        var result = await _strategicGoalService.UpdateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a strategic goal by identifier.
    /// </summary>
    /// <param name="id">Strategic goal identifier.</param>
    /// <response code="204">Strategic goal deleted successfully.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _strategicGoalService.RemoveAsync(id);
        return NoContent();
    }
}

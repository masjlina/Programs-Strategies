using Application.Dtos;
using Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Manages operational goals.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OperationalGoalsController : ControllerBase
{
    private readonly IOperationalGoalService _operationalGoalService;

    public OperationalGoalsController(IOperationalGoalService operationalGoalService)
    {
        _operationalGoalService = operationalGoalService;
    }

    /// <summary>
    /// Returns all operational goals.
    /// </summary>
    /// <response code="200">List of operational goals.</response>
    [HttpGet]
    public async Task<ActionResult<List<OperationalGoalDto>>> GetAll()
    {
        var result = await _operationalGoalService.GetAllAsync();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new operational goal.
    /// </summary>
    /// <param name="dto">Operational goal payload.</param>
    /// <response code="200">Operational goal created successfully.</response>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<OperationalGoalDto>> Create([FromBody] OperationalGoalDto dto)
    {
        var result = await _operationalGoalService.CreateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing operational goal.
    /// </summary>
    /// <param name="id">Operational goal identifier.</param>
    /// <param name="dto">Operational goal payload.</param>
    /// <response code="200">Operational goal updated successfully.</response>
    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OperationalGoalDto>> Update(Guid id, [FromBody] OperationalGoalDto dto)
    {
        dto.Id = id;
        var result = await _operationalGoalService.UpdateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Deletes an operational goal by identifier.
    /// </summary>
    /// <param name="id">Operational goal identifier.</param>
    /// <response code="204">Operational goal deleted successfully.</response>
    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _operationalGoalService.RemoveAsync(id);
        return NoContent();
    }
}

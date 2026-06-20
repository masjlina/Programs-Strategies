using Application.Dtos;
using Application.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Manages program tasks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProgramTasksController : ControllerBase
{
    private readonly IProgramTaskService _programTaskService;

    public ProgramTasksController(IProgramTaskService programTaskService)
    {
        _programTaskService = programTaskService;
    }

    /// <summary>
    /// Returns all program tasks.
    /// </summary>
    /// <response code="200">List of program tasks.</response>
    [HttpGet]
    public async Task<ActionResult<List<ProgramTaskDto>>> GetAll()
    {
        var result = await _programTaskService.GetAllAsync();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new program task.
    /// </summary>
    /// <param name="dto">Program task payload.</param>
    /// <response code="200">Program task created successfully.</response>
    [HttpPost]
    public async Task<ActionResult<ProgramTaskDto>> Create([FromBody] ProgramTaskDto dto)
    {
        var result = await _programTaskService.CreateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing program task.
    /// </summary>
    /// <param name="id">Program task identifier.</param>
    /// <param name="dto">Program task payload.</param>
    /// <response code="200">Program task updated successfully.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProgramTaskDto>> Update(Guid id, [FromBody] ProgramTaskDto dto)
    {
        dto.Id = id;
        var result = await _programTaskService.UpdateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a program task by identifier.
    /// </summary>
    /// <param name="id">Program task identifier.</param>
    /// <response code="204">Program task deleted successfully.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _programTaskService.RemoveAsync(id);
        return NoContent();
    }
}

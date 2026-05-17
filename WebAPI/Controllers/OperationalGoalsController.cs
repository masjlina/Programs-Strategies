using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperationalGoalsController : ControllerBase
{
    private readonly IOperationalGoalService _operationalGoalService;

    public OperationalGoalsController(IOperationalGoalService operationalGoalService)
    {
        _operationalGoalService = operationalGoalService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OperationalGoalDto>>> GetAll()
    {
        var result = await _operationalGoalService.GetAllAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OperationalGoalDto>> Create([FromBody] OperationalGoalDto dto)
    {
        var result = await _operationalGoalService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OperationalGoalDto>> Update(Guid id, [FromBody] OperationalGoalDto dto)
    {
        dto.Id = id;
        var result = await _operationalGoalService.UpdateAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _operationalGoalService.RemoveAsync(id);
        return NoContent();
    }
}

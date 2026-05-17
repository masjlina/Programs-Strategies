using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StrategicGoalsController : ControllerBase
{
    private readonly IStrategicGoalService _strategicGoalService;

    public StrategicGoalsController(IStrategicGoalService strategicGoalService)
    {
        _strategicGoalService = strategicGoalService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StrategicGoalDto>>> GetAll()
    {
        var result = await _strategicGoalService.GetAllAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<StrategicGoalDto>> Create([FromBody] StrategicGoalDto dto)
    {
        var result = await _strategicGoalService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StrategicGoalDto>> Update(Guid id, [FromBody] StrategicGoalDto dto)
    {
        dto.Id = id;
        var result = await _strategicGoalService.UpdateAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _strategicGoalService.RemoveAsync(id);
        return NoContent();
    }
}

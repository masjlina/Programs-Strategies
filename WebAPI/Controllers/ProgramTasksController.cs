using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProgramTasksController : ControllerBase
{
    private readonly IProgramTaskService _programTaskService;

    public ProgramTasksController(IProgramTaskService programTaskService)
    {
        _programTaskService = programTaskService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProgramTaskDto>>> GetAll()
    {
        var result = await _programTaskService.GetAllAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProgramTaskDto>> Create([FromBody] ProgramTaskDto dto)
    {
        var result = await _programTaskService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProgramTaskDto>> Update(Guid id, [FromBody] ProgramTaskDto dto)
    {
        dto.Id = id;
        var result = await _programTaskService.UpdateAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _programTaskService.RemoveAsync(id);
        return NoContent();
    }
}

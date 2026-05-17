using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StrategiesController : ControllerBase
{
    private readonly IStrategyService _strategyService;

    public StrategiesController(IStrategyService strategyService)
    {
        _strategyService = strategyService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StrategyDto>>> GetAll()
    {
        var result = await _strategyService.GetAllAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<StrategyDto>> Create([FromBody] StrategyDto dto)
    {
        var result = await _strategyService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StrategyDto>> Update(Guid id, [FromBody] StrategyDto dto)
    {
        dto.Id = id;
        var result = await _strategyService.UpdateAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _strategyService.RemoveAsync(id);
        return NoContent();
    }
}

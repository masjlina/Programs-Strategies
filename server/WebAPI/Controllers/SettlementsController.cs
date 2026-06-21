using Application.Dtos;
using Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettlementsController : ControllerBase
{
    private readonly ICrudService<SettlementDto> _service;

    public SettlementsController(ICrudService<SettlementDto> service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<SettlementDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<SettlementDto>> Create([FromBody] SettlementDto dto)
    {
        return Ok(await _service.CreateAsync(dto));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SettlementDto>> Update(Guid id, [FromBody] SettlementDto dto)
    {
        dto.Id = id;
        return Ok(await _service.UpdateAsync(dto));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.RemoveAsync(id);
        return NoContent();
    }
}

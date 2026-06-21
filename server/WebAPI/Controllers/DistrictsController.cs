using Application.Dtos;
using Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DistrictsController : ControllerBase
{
    private readonly ICrudService<DistrictDto> _service;

    public DistrictsController(ICrudService<DistrictDto> service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<DistrictDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<DistrictDto>> Create([FromBody] DistrictDto dto)
    {
        return Ok(await _service.CreateAsync(dto));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DistrictDto>> Update(Guid id, [FromBody] DistrictDto dto)
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

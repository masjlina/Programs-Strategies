using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdministrativeUnitsController : ControllerBase
{
    private readonly IAdministrativeUnitService _administrativeUnitService;

    public AdministrativeUnitsController(IAdministrativeUnitService administrativeUnitService)
    {
        _administrativeUnitService = administrativeUnitService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdministrativeUnitDto>>> GetAll()
    {
        var result = await _administrativeUnitService.GetAllAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdministrativeUnitDto>> Create([FromBody] AdministrativeUnitDto dto)
    {
        var result = await _administrativeUnitService.CreateAsync(dto);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdministrativeUnitDto>> Update(Guid id, [FromBody] AdministrativeUnitDto dto)
    {
        dto.Id = id;
        var result = await _administrativeUnitService.UpdateAsync(dto);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _administrativeUnitService.RemoveAsync(id);
        return NoContent();
    }
}

using ApplicationCore.Dtos;
using ApplicationCore.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Manages administrative units.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdministrativeUnitsController : ControllerBase
{
    private readonly IAdministrativeUnitService _administrativeUnitService;

    public AdministrativeUnitsController(IAdministrativeUnitService administrativeUnitService)
    {
        _administrativeUnitService = administrativeUnitService;
    }

    /// <summary>
    /// Returns all administrative units.
    /// </summary>
    /// <response code="200">List of administrative units.</response>
    [HttpGet]
    public async Task<ActionResult<List<AdministrativeUnitDto>>> GetAll()
    {
        var result = await _administrativeUnitService.GetAllAsync();
        return Ok(result);
    }

    /// <summary>
    /// Creates a new administrative unit.
    /// </summary>
    /// <param name="dto">Administrative unit payload.</param>
    /// <response code="200">Administrative unit created successfully.</response>
    [HttpPost]
    public async Task<ActionResult<AdministrativeUnitDto>> Create([FromBody] AdministrativeUnitDto dto)
    {
        var result = await _administrativeUnitService.CreateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Updates an existing administrative unit.
    /// </summary>
    /// <param name="id">Administrative unit identifier.</param>
    /// <param name="dto">Administrative unit payload.</param>
    /// <response code="200">Administrative unit updated successfully.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdministrativeUnitDto>> Update(Guid id, [FromBody] AdministrativeUnitDto dto)
    {
        dto.Id = id;
        var result = await _administrativeUnitService.UpdateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Deletes an administrative unit by identifier.
    /// </summary>
    /// <param name="id">Administrative unit identifier.</param>
    /// <response code="204">Administrative unit deleted successfully.</response>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _administrativeUnitService.RemoveAsync(id);
        return NoContent();
    }
}

using Application.Dtos;
using Application.Services.IServices;
using Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommunitiesController : ControllerBase
{
    private readonly ICrudService<CommunityDto> _service;
    private readonly ApplicationDbContext _dbContext;

    public CommunitiesController(ICrudService<CommunityDto> service, ApplicationDbContext dbContext)
    {
        _service = service;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommunityDto>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPost]
    public async Task<ActionResult<CommunityDto>> Create([FromBody] CommunityDto dto)
    {
        return Ok(await _service.CreateAsync(dto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CommunityDto>> Update(Guid id, [FromBody] CommunityDto dto)
    {
        dto.Id = id;
        return Ok(await _service.UpdateAsync(dto));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateStrategiesUrl(Guid id, [FromBody] UpdateStrategiesUrlDto dto)
    {
        if (dto == null)
        {
            return BadRequest(new { message = "Некоректний запит" });
        }

        var community = await _dbContext.Communities.SingleOrDefaultAsync(x => x.Id == id);
        if (community == null)
        {
            return NotFound(new { message = "Громада не знайдена" });
        }

        if (!string.IsNullOrEmpty(dto.StrategiesUrl))
        {
            if (!Uri.TryCreate(dto.StrategiesUrl, UriKind.Absolute, out var uriResult) 
                || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new { message = "Некоректний формат URL для стратегії. URL має починатися з http:// або https://" });
            }
        }

        if (!string.IsNullOrEmpty(dto.WebsiteUrl))
        {
            if (!Uri.TryCreate(dto.WebsiteUrl, UriKind.Absolute, out var uriResult) 
                || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new { message = "Некоректний формат URL для сайту. URL має починатися з http:// або https://" });
            }
        }

        community.StrategiesUrl = dto.StrategiesUrl;
        community.WebsiteUrl = dto.WebsiteUrl;
        await _dbContext.SaveChangesAsync();

        return Ok(new { 
            message = "Дані успішно оновлено", 
            strategiesUrl = community.StrategiesUrl,
            websiteUrl = community.WebsiteUrl
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.RemoveAsync(id);
        return NoContent();
    }
}

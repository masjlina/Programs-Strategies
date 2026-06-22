using Application.Dtos;
using Application.Services.IServices;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

/// <summary>
/// Public system statistics for the home dashboard.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StatsController : ControllerBase
{
    private readonly ISystemStatsService _systemStatsService;

    public StatsController(ISystemStatsService systemStatsService)
    {
        _systemStatsService = systemStatsService;
    }

    /// <summary>
    /// Returns aggregated statistics about regions, communities, and strategies.
    /// </summary>
    /// <response code="200">System statistics.</response>
    [HttpGet]
    public async Task<ActionResult<SystemStatsDto>> Get(CancellationToken cancellationToken)
    {
        var result = await _systemStatsService.GetStatsAsync(cancellationToken);
        return Ok(result);
    }
}

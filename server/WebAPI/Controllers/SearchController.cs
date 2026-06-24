using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Application.Dtos;
using Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public SearchController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<SearchResultDto>> Search(
        [FromQuery] string? query = null,
        [FromQuery] string? filter = null,
        [FromQuery] string? sort = null,
        [FromQuery] Guid? regionId = null,
        [FromQuery] Guid? districtId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 30;

        var searchItems = new List<SearchItemDto>();

        // 1. Determine which entity types to load based on the filter
        bool loadRegions = string.IsNullOrEmpty(filter) || filter == "all" || filter == "region";
        bool loadDistricts = false; // Disabled because districts do not have strategies
        bool loadCommunities = string.IsNullOrEmpty(filter) || filter == "all" || filter == "community";

        // 2. Load Regions
        if (loadRegions)
        {
            var regions = await _dbContext.Regions
                .Include(r => r.Strategies)
                .AsNoTracking()
                .ToListAsync();

            foreach (var r in regions)
            {
                var name = string.IsNullOrEmpty(r.NameFull) ? r.Name : r.NameFull;
                searchItems.Add(new SearchItemDto
                {
                    Id = r.Id,
                    Name = name,
                    Type = "Region",
                    RegionId = r.Id,
                    RegionName = name,
                    DistrictName = "",
                    Strategies = r.Strategies.Select(s => new SearchStrategyDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        RegionId = s.RegionId,
                        DistrictId = s.DistrictId,
                        CommunityId = s.CommunityId
                    }).ToList()
                });
            }
        }

        // 3. Load Districts
        if (loadDistricts)
        {
            var districts = await _dbContext.Districts
                .Include(d => d.Strategies)
                .Include(d => d.Region)
                .AsNoTracking()
                .ToListAsync();

            foreach (var d in districts)
            {
                var name = string.IsNullOrEmpty(d.NameFull) ? d.Name : d.NameFull;
                var regionName = d.Region != null ? (string.IsNullOrEmpty(d.Region.NameFull) ? d.Region.Name : d.Region.NameFull) : "";
                searchItems.Add(new SearchItemDto
                {
                    Id = d.Id,
                    Name = name,
                    Type = "District",
                    RegionId = d.RegionId,
                    DistrictId = d.Id,
                    RegionName = regionName,
                    DistrictName = name,
                    Strategies = d.Strategies.Select(s => new SearchStrategyDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        RegionId = s.RegionId,
                        DistrictId = s.DistrictId,
                        CommunityId = s.CommunityId
                    }).ToList()
                });
            }
        }

        // 4. Load Communities
        if (loadCommunities)
        {
            var communities = await _dbContext.Communities
                .Include(c => c.Strategies)
                .Include(c => c.Region)
                .Include(c => c.District)
                .AsNoTracking()
                .ToListAsync();

            foreach (var c in communities)
            {
                var name = string.IsNullOrEmpty(c.NameFull) ? c.Name : c.NameFull;
                var regionName = c.Region != null ? (string.IsNullOrEmpty(c.Region.NameFull) ? c.Region.Name : c.Region.NameFull) : "";
                var districtName = c.District != null ? (string.IsNullOrEmpty(c.District.NameFull) ? c.District.Name : c.District.NameFull) : "";
                searchItems.Add(new SearchItemDto
                {
                    Id = c.Id,
                    Name = name,
                    Type = "Community",
                    RegionId = c.RegionId,
                    DistrictId = c.DistrictId,
                    CommunityId = c.Id,
                    RegionName = regionName,
                    DistrictName = districtName,
                    Strategies = c.Strategies.Select(s => new SearchStrategyDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        RegionId = s.RegionId,
                        DistrictId = s.DistrictId,
                        CommunityId = s.CommunityId
                    }).ToList()
                });
            }
        }

        // Apply region and district filter
        if (regionId.HasValue)
        {
            searchItems = searchItems.Where(item => item.RegionId == regionId.Value).ToList();
        }
        if (districtId.HasValue)
        {
            searchItems = searchItems.Where(item => item.DistrictId == districtId.Value).ToList();
        }

        // 5. Apply query filter
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower(CultureInfo.InvariantCulture);
            searchItems = searchItems.Where(item =>
                item.Name.ToLower(CultureInfo.InvariantCulture).Contains(q) ||
                item.RegionName.ToLower(CultureInfo.InvariantCulture).Contains(q) ||
                item.DistrictName.ToLower(CultureInfo.InvariantCulture).Contains(q) ||
                item.Strategies.Any(s => s.Title.ToLower(CultureInfo.InvariantCulture).Contains(q))
            ).ToList();
        }

        // 6. Apply sorting
        var cultureInfo = new CultureInfo("uk-UA");
        var comparer = StringComparer.Create(cultureInfo, true);

        if (sort == "programs-desc")
        {
            searchItems = searchItems
                .OrderByDescending(item => item.Strategies.Count)
                .ThenBy(item => item.Name, comparer)
                .ToList();
        }
        else if (sort == "programs-asc")
        {
            searchItems = searchItems
                .OrderBy(item => item.Strategies.Count)
                .ThenBy(item => item.Name, comparer)
                .ToList();
        }
        else // "name-asc" or default
        {
            searchItems = searchItems
                .OrderBy(item => item.Name, comparer)
                .ToList();
        }

        // 7. Paginate
        var totalCount = searchItems.Count;
        var paginatedItems = searchItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(new SearchResultDto
        {
            Items = paginatedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}

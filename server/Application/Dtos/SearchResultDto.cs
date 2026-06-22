using System;
using System.Collections.Generic;

namespace Application.Dtos;

public class SearchStrategyDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public Guid? RegionId { get; set; }
    public Guid? DistrictId { get; set; }
    public Guid? CommunityId { get; set; }
}

public class SearchItemDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; } // "Region" | "District" | "Community"
    public Guid? RegionId { get; set; }
    public Guid? DistrictId { get; set; }
    public Guid? CommunityId { get; set; }
    public required string RegionName { get; set; }
    public required string DistrictName { get; set; }
    public List<SearchStrategyDto> Strategies { get; set; } = new();
}

public class SearchResultDto
{
    public List<SearchItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

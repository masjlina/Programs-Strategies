using System;
using System.Collections.Generic;

namespace Application.Dtos;

public class KeywordIntensityDto
{
    public required string Keyword { get; set; }
    public required KeywordStatisticsDto Statistics { get; set; }
    public required List<KeywordIntensityItemDto> Items { get; set; }
}

public class KeywordStatisticsDto
{
    public double GlobalMean { get; set; }
    public double Variance { get; set; }
}

public class KeywordIntensityItemDto
{
    public Guid StrategyId { get; set; }
    public required string StrategyTitle { get; set; }
    public required string TargetName { get; set; }
    public required string Level { get; set; }
    public int Count { get; set; }
    public double Deviation { get; set; }
}

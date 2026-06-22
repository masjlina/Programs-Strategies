namespace Application.Dtos;

public class SystemStatsDto
{
    public int RegionsCount { get; set; }
    public int CommunitiesCount { get; set; }
    public int TotalStrategiesCount { get; set; }
    public int CommunitiesWithoutStrategiesCount { get; set; }
    public double AverageStrategiesPerCommunity { get; set; }
    public int CommunitiesWithWebsiteCount { get; set; }
    public double CommunitiesWithWebsitePercent { get; set; }
    public List<DailyStrategyCountDto> StrategiesLastMonthByDay { get; set; } = new();
}

public class DailyStrategyCountDto
{
    public required string Date { get; set; }
    public int Count { get; set; }
}

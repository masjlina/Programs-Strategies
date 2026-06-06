namespace ApplicationCore.Dtos;

public class StrategyDto : BaseDto
{
    public Guid? RegionId { get; set; }
    public Guid? DistrictId { get; set; }
    public Guid? CommunityId { get; set; }

    /// <summary>
    /// Strategy title.
    /// </summary>
    public required string Title { get; set; }

    public string? StrategyUrl { get; set; }

    /// <summary>
    /// Strategic goals included in this strategy.
    /// </summary>
    public List<StrategicGoalDto> StrategicGoals { get; set; } = new();
}

namespace Domain.Entities;

public class Strategy : BaseEntity
{
    public required string Title { get; set; }
    public Guid? RegionId { get; set; }
    public Region? Region { get; set; }
    public Guid? DistrictId { get; set; }
    public District? District { get; set; }
    public Guid? CommunityId { get; set; }
    public Community? Community { get; set; }
    public string? StrategyUrl { get; set; }
    public List<StrategicGoal> StrategicGoals { get; set; } = new();
}

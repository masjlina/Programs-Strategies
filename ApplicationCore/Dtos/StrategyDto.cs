namespace ApplicationCore.Dtos;

public class StrategyDto : BaseDto
{
    public Guid AdministrativeUnitId { get; set; }
    public required string Title { get; set; }
    public List<StrategicGoalDto> StrategicGoals { get; set; } = new();
}

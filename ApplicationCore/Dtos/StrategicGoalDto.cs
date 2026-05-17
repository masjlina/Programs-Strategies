namespace ApplicationCore.Dtos;

public class StrategicGoalDto : BaseDto
{
    public Guid StrategyId { get; set; }
    public required string Label { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public List<OperationalGoalDto> OperationalGoals { get; set; } = new();
}

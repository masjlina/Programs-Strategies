namespace Domain.Entities;

public class StrategicGoal : BaseEntity
{
    public Guid StrategyId { get; set; }
    public Strategy? Strategy { get; set; }
    public required string Label { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public List<OperationalGoal> OperationalGoals { get; set; } = new();
}

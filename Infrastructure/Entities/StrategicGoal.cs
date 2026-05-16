namespace Infrastructure.Entities;

public class StrategicGoal : BaseEntity
{
    public Guid StrategyId { get; set; }
    public string Label { get; set; }
    public int Number { get; set; }
    public string Title { get; set; }
    public List<OperationalGoal> OperationalGoals { get; set; } = new List<OperationalGoal>();
}
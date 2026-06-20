namespace Domain.Entities;

public class OperationalGoal : BaseEntity
{
    public Guid StrategicGoalId { get; set; }
    public StrategicGoal? StrategicGoal { get; set; }
    public required string Label { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public List<ProgramTask> ProgramTasks { get; set; } = new();
}

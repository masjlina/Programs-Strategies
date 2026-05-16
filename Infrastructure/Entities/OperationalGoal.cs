namespace Infrastructure.Entities;

public class OperationalGoal : BaseEntity
{
    public Guid StrategicGoalId { get; set; }
    public string Label { get; set; }
    public int Number { get; set; }
    public string Title { get; set; }
    public List<ProgramTask> ProgramTasks { get; set; } = new List<ProgramTask>();
}
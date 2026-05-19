namespace Infrastructure.Entities;

public class ProgramTask : BaseEntity
{
    public Guid OperationalGoalId { get; set; }
    public OperationalGoal? OperationalGoal { get; set; }
    public required string Label { get; set; }
    public int Number { get; set; }
    public required string Description { get; set; }
}

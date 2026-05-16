namespace Infrastructure.Entities;

public class ProgramTask : BaseEntity
{
    public Guid OperationalGoalId { get; set; }
    public string Label { get; set; }
    public int Number { get; set; }
    public string Description { get; set; }
}
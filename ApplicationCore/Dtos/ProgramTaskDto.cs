namespace ApplicationCore.Dtos;

public class ProgramTaskDto : BaseDto
{
    public Guid OperationalGoalId { get; set; }
    public required string Label { get; set; }
    public int Number { get; set; }
    public required string Description { get; set; }
}

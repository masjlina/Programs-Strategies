namespace ApplicationCore.Dtos;

public class OperationalGoalDto : BaseDto
{
    public Guid StrategicGoalId { get; set; }
    public required string Label { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public List<ProgramTaskDto> ProgramTasks { get; set; } = new();
}

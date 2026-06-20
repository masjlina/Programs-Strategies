namespace Application.Dtos;

/// <summary>
/// Program task inside an operational goal.
/// </summary>
public class ProgramTaskDto : BaseDto
{
    /// <summary>
    /// Parent operational goal identifier.
    /// </summary>
    public Guid OperationalGoalId { get; set; }

    /// <summary>
    /// Human-readable label for the program task.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Sequence number of the program task.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Detailed description of the program task.
    /// </summary>
    public required string Description { get; set; }
}

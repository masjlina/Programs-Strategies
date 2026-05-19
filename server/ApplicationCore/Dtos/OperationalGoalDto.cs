namespace ApplicationCore.Dtos;

/// <summary>
/// Operational goal within a strategic goal.
/// </summary>
public class OperationalGoalDto : BaseDto
{
    /// <summary>
    /// Parent strategic goal identifier.
    /// </summary>
    public Guid StrategicGoalId { get; set; }

    /// <summary>
    /// Human-readable label for the operational goal.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Sequence number of the operational goal.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Operational goal title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Program tasks related to this operational goal.
    /// </summary>
    public List<ProgramTaskDto> ProgramTasks { get; set; } = new();
}

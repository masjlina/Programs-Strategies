namespace Application.Dtos;

/// <summary>
/// Strategic goal within a strategy.
/// </summary>
public class StrategicGoalDto : BaseDto
{
    /// <summary>
    /// Parent strategy identifier.
    /// </summary>
    public Guid StrategyId { get; set; }

    /// <summary>
    /// Human-readable label for the strategic goal.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// Sequence number of the strategic goal.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Strategic goal title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Operational goals included in this strategic goal.
    /// </summary>
    public List<OperationalGoalDto> OperationalGoals { get; set; } = new();
}

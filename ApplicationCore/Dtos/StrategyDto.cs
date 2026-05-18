namespace ApplicationCore.Dtos;

/// <summary>
/// Strategy owned by an administrative unit.
/// </summary>
public class StrategyDto : BaseDto
{
    /// <summary>
    /// Parent administrative unit identifier.
    /// </summary>
    public Guid AdministrativeUnitId { get; set; }

    /// <summary>
    /// Strategy title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Strategic goals included in this strategy.
    /// </summary>
    public List<StrategicGoalDto> StrategicGoals { get; set; } = new();
}

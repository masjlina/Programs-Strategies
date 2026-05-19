namespace Infrastructure.Entities;

public class Strategy : BaseEntity
{
    public required string Title { get; set; }
    public Guid AdministrativeUnitId { get; set; }
    public AdministrativeUnit? AdministrativeUnit { get; set; }
    public List<StrategicGoal> StrategicGoals { get; set; } = new();
}

namespace Infrastructure.Entities;

public class Strategy : BaseEntity
{
    public string Title { get; set; }
    public Guid AdministrativeUnitId { get; set; }
    public List<StrategicGoal> StrategicGoals { get; set; } = new List<StrategicGoal>();
}
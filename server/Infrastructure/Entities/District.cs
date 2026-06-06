namespace Infrastructure.Entities;

public class District : AdministrativeEntity
{
    public Guid RegionId { get; set; }
    public Region? Region { get; set; }
    public List<Community> Communities { get; set; } = new();
    public List<Settlement> Settlements { get; set; } = new();
    public List<Strategy> Strategies { get; set; } = new();
}

namespace Domain.Entities;

public class Community : AdministrativeEntity
{
    public Guid RegionId { get; set; }
    public Region? Region { get; set; }
    public Guid DistrictId { get; set; }
    public District? District { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? StrategiesUrl { get; set; }
    public List<Settlement> Settlements { get; set; } = new();
    public List<Strategy> Strategies { get; set; } = new();
}

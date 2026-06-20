namespace Domain.Entities;

public class Region : AdministrativeEntity
{
    public string? WebsiteUrl { get; set; }
    public string? StrategiesUrl { get; set; }
    public List<District> Districts { get; set; } = new();
    public List<Community> Communities { get; set; } = new();
    public List<Settlement> Settlements { get; set; } = new();
    public List<Strategy> Strategies { get; set; } = new();
}

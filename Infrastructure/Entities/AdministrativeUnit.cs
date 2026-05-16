using Shared;

namespace Infrastructure.Entities;

public class AdministrativeUnit : BaseEntity
{
    public required string Name { get; set; }
    public required AdministrativeUnitType Type { get; set; }
    public List<Strategy> Strategies { get; set; } = new();
}
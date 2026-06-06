namespace Infrastructure.Entities;

public abstract class AdministrativeEntity : BaseEntity
{
    public required string KattotgId { get; set; }
    public required string Name { get; set; }
    public required string NameFull { get; set; }
    public required string Category { get; set; }
    public string? ImgUrl { get; set; }
}

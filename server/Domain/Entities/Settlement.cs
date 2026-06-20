namespace Domain.Entities;

public class Settlement : BaseEntity
{
    public required string KattotgId { get; set; }
    public Guid RegionId { get; set; }
    public Region? Region { get; set; }
    public Guid? CommunityId { get; set; }
    public Community? Community { get; set; }
    public Guid? DistrictId { get; set; }
    public District? District { get; set; }
    public Guid? ParentSettlementId { get; set; }
    public Settlement? ParentSettlement { get; set; }
    public required string Name { get; set; }
    public required string NameFull { get; set; }
    public required string ParentType { get; set; }
    public List<Settlement> ChildSettlements { get; set; } = new();
}

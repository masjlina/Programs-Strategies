namespace ApplicationCore.Dtos;

public class DistrictDto : AdministrativeDto
{
    public Guid RegionId { get; set; }
    public List<CommunityDto> Communities { get; set; } = new();
    public List<SettlementDto> Settlements { get; set; } = new();
    public List<StrategyDto> Strategies { get; set; } = new();
}

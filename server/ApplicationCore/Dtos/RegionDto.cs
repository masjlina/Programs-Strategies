using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Dtos;

public class RegionDto : AdministrativeDto
{
    [MaxLength(512)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(512)]
    public string? StrategiesUrl { get; set; }

    public List<DistrictDto> Districts { get; set; } = new();
    public List<CommunityDto> Communities { get; set; } = new();
    public List<SettlementDto> Settlements { get; set; } = new();
    public List<StrategyDto> Strategies { get; set; } = new();
}

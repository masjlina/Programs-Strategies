using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class CommunityDto : AdministrativeDto
{
    public Guid RegionId { get; set; }
    public Guid DistrictId { get; set; }

    [MaxLength(512)]
    public string? WebsiteUrl { get; set; }

    [MaxLength(512)]
    public string? StrategiesUrl { get; set; }

    public List<SettlementDto> Settlements { get; set; } = new();
    public List<StrategyDto> Strategies { get; set; } = new();
}

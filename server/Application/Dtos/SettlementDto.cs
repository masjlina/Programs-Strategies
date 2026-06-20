using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class SettlementDto : BaseDto
{
    [Required]
    [MaxLength(20)]
    public required string KattotgId { get; set; }

    public Guid RegionId { get; set; }
    public Guid? CommunityId { get; set; }
    public Guid? DistrictId { get; set; }
    public Guid? ParentSettlementId { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public required string NameFull { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ParentType { get; set; }

    public List<SettlementDto> ChildSettlements { get; set; } = new();
}

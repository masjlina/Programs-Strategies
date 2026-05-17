using System.ComponentModel.DataAnnotations;
using Shared;

namespace ApplicationCore.Dtos;

public class AdministrativeUnitDto : BaseDto
{
    [Required(ErrorMessage = "Administrative unit name is required")]
    [MaxLength(150, ErrorMessage = "Administrative unit name cannot exceed 150 characters")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Administrative unit type is required")]
    public required AdministrativeUnitType Type { get; set; }

    public List<StrategyDto>? Strategies { get; set; }
}
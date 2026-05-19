using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Shared;

namespace ApplicationCore.Dtos;

/// <summary>
/// Administrative unit with its type and related strategies.
/// </summary>
public class AdministrativeUnitDto : BaseDto
{
    /// <summary>
    /// Administrative unit name.
    /// </summary>
    [Required(ErrorMessage = "Administrative unit name is required")]
    [MaxLength(150, ErrorMessage = "Administrative unit name cannot exceed 150 characters")]
    public required string Name { get; set; }

    /// <summary>
    /// Administrative unit type.
    /// </summary>
    [Required(ErrorMessage = "Administrative unit type is required")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required AdministrativeUnitType Type { get; set; }

    /// <summary>
    /// Strategies that belong to this administrative unit.
    /// </summary>
    public List<StrategyDto>? Strategies { get; set; }
}

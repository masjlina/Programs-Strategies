using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public abstract class AdministrativeDto : BaseDto
{
    [Required]
    [MaxLength(20)]
    public required string KattotgId { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public required string NameFull { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Category { get; set; }

    [MaxLength(512)]
    public string? ImgUrl { get; set; }
}

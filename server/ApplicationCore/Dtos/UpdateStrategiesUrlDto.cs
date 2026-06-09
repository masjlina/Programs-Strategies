using System.ComponentModel.DataAnnotations;

namespace ApplicationCore.Dtos;

public class UpdateStrategiesUrlDto
{
    [Url(ErrorMessage = "Некоректний формат URL")]
    [MaxLength(512)]
    public string? StrategiesUrl { get; set; }

    [Url(ErrorMessage = "Некоректний формат URL")]
    [MaxLength(512)]
    public string? WebsiteUrl { get; set; }
}

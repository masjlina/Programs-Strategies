namespace WebAPI.Services;

public class ApiSettings
{
    public string ValidIssuer { get; set; } = string.Empty;
    public string[]? ValidAudiences { get; set; }
    public string SigningKey { get; set; } = string.Empty;
}

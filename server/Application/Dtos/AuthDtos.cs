using System.ComponentModel.DataAnnotations;

namespace Application.Dtos;

public class SignInRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class SignInResponseDto
{
    public UserDto User { get; set; } = null!;
    public string AccessToken { get; set; } = string.Empty;
}

public class RefreshResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
}

public class CheckAuthResponseDto
{
    public UserDto User { get; set; } = null!;
}

public class ErrorResponse
{
    public IEnumerable<string> Errors { get; set; } = [];
}

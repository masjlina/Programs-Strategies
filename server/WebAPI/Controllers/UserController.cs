using System.Security.Claims;
using Domain.Data;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Application.Dtos;
using WebAPI.Services;

namespace WebAPI.Controllers;

[ApiController]
[Route("api")]
public class UserController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _dbContext;


    public UserController(ITokenService tokenService, ApplicationDbContext dbContext)
    {
        _tokenService = tokenService;
        _dbContext = dbContext;
    }

    [HttpPost]
    [Route("sign-in")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequestDto? signInRequestDto)
    {
        if (signInRequestDto == null || !ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == signInRequestDto.Email);
        if (user == null)
        {
            return Unauthorized(new ErrorResponse
            {
                Errors = new[] { "Invalid email or password" }
            });
        }

        var isPasswordValid = PasswordHasher.VerifyPassword(signInRequestDto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return Unauthorized(new ErrorResponse
            {
                Errors = new[] { "Invalid email or password" }
            });
        }

        var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        string accessToken = _tokenService.GenerateAccessToken(userClaims);
        string refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiration = GetRefreshTokenExpiration();

        var tokenInfo = await _dbContext.TokenInfos.FirstOrDefaultAsync(a => a.UserId == user.Id);

        if (tokenInfo == null)
        {
            var ti = new TokenInfo
            {
                UserId = user.Id,
                RefreshToken = refreshToken,
                ExpiredAt = refreshTokenExpiration.UtcDateTime
            };
            _dbContext.TokenInfos.Add(ti);
        }
        else
        {
            tokenInfo.RefreshToken = refreshToken;
            tokenInfo.ExpiredAt = refreshTokenExpiration.UtcDateTime;
        }

        await _dbContext.SaveChangesAsync();

        Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = refreshTokenExpiration,
            Path = "/"
        });

        return Ok(new SignInResponseDto
        {
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role
            },
            AccessToken = accessToken
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var userId))
            {
                await _dbContext.TokenInfos
                    .Where(t => t.UserId == userId && t.RefreshToken == refreshToken)
                    .ExecuteDeleteAsync();
            }
        }

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            Path = "/"
        });

        return Ok();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> CheckAuth()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new ErrorResponse
            {
                Errors = new[] { "Id does not exist" }
            });

        var user = await _dbContext.Users.FindAsync(userId);
        if (user is null)
            return Unauthorized(new ErrorResponse
            {
                Errors = new[] { "User does not exist" }
            });

        return Ok(new CheckAuthResponseDto
        {
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role
            }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
            return Unauthorized("Refresh token missing");

        var tokenInfo = await _dbContext.TokenInfos
            .SingleOrDefaultAsync(t =>
                t.RefreshToken == refreshToken &&
                t.ExpiredAt > DateTime.UtcNow);

        if (tokenInfo == null)
            return Unauthorized(new ErrorResponse
            {
                Errors = new[] { "Invalid refresh token" }
            });

        var user = await _dbContext.Users.FindAsync(tokenInfo.UserId);
        if (user == null)
            return Unauthorized();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var newAccessToken = _tokenService.GenerateAccessToken(claims);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        tokenInfo.RefreshToken = newRefreshToken;

        await _dbContext.SaveChangesAsync();

        Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = tokenInfo.ExpiredAt,
            Path = "/"
        });

        return Ok(new RefreshResponseDto
        {
            AccessToken = newAccessToken
        });
    }

    private static DateTimeOffset GetRefreshTokenExpiration()
    {
        return DateTimeOffset.UtcNow.AddDays(30);
    }
}
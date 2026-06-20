using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI.Services;

public class TokenService : ITokenService
{
    private readonly ApiSettings _apiSettings;

    public TokenService(IOptions<ApiSettings> apiSettings)
    {
        _apiSettings = apiSettings.Value;
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var audience = _apiSettings.ValidAudiences?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Authentication:Schemes:Bearer:ValidAudiences must contain at least one value.");
        }

        var signingKey = new SymmetricSecurityKey(Convert.FromBase64String(_apiSettings.SigningKey));

        var tokenOptions = new JwtSecurityToken(
            issuer: _apiSettings.ValidIssuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(tokenOptions);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];

        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            randomNumberGenerator.GetBytes(randomNumber);
        }

        return Convert.ToBase64String(randomNumber);
    }
}

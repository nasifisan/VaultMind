using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(Guid userId, string name, string email, string role)
    {
        var secret = _configuration["Auth:JwtSecret"] ?? "your-super-secret-vaultmind-jwt-signing-key-2026-must-be-long-enough";
        var issuer = _configuration["Auth:JwtIssuer"] ?? "VaultMind.API";
        var audience = _configuration["Auth:JwtAudience"] ?? "VaultMind.Dashboard";
        var expirationMinutes = double.Parse(_configuration["Auth:AccessTokenExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("uniquename", name) // Map custom uniquename
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid GenerateRefreshToken()
    {
        return Guid.NewGuid();
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var secret = _configuration["Auth:JwtSecret"] ?? "your-super-secret-vaultmind-jwt-signing-key-2026-must-be-long-enough";
        var issuer = _configuration["Auth:JwtIssuer"] ?? "VaultMind.API";
        var audience = _configuration["Auth:JwtAudience"] ?? "VaultMind.Dashboard";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true, // Force expiration check
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null; // Invalid token
        }
    }
}

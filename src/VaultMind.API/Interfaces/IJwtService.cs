using System;
using System.Security.Claims;

namespace VaultMind.API.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string name, string email, string role);
    Guid GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using VaultMind.API.Interfaces;
using VaultMind.API.Models;

namespace VaultMind.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMongoRepository<User> _usersRepo;
    private readonly IMongoRepository<RefreshToken> _refreshTokensRepo;
    private readonly IMongoRepository<ActiveAccessToken> _activeTokensRepo;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _config;

    public AuthController(
        IMongoRepository<User> usersRepo,
        IMongoRepository<RefreshToken> refreshTokensRepo,
        IMongoRepository<ActiveAccessToken> activeTokensRepo,
        IJwtService jwtService,
        IConfiguration config)
    {
        _usersRepo = usersRepo;
        _refreshTokensRepo = refreshTokensRepo;
        _activeTokensRepo = activeTokensRepo;
        _jwtService = jwtService;
        _config = config;
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] TokenRequest request)
    {
        var refreshTokenExpiryDays = double.Parse(_config["Auth:RefreshTokenExpirationDays"] ?? "7");
        var accessTokenExpiryMinutes = double.Parse(_config["Auth:AccessTokenExpirationMinutes"] ?? "60");

        // ── Refresh Token Flow ──
        if (request.RefreshToken != null && request.RefreshToken != Guid.Empty)
        {
            var storedToken = await _refreshTokensRepo.FindOneAsync(
                t => t.Token == request.RefreshToken && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow
            );

            if (storedToken == null)
            {
                return Unauthorized(new { Error = "Invalid or expired refresh token" });
            }

            // Revoke the old refresh token (by marking it revoked, or deleting it)
            await _refreshTokensRepo.UpdateOneAsync(
                t => t.Id == storedToken.Id,
                Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true).Set(t => t.ExpiresAt, DateTime.UtcNow)
            );

            // Determine if the original token belonged to an anonymous user or registered user
            Guid userId = storedToken.UserId;
            string name, email, role;

            if (userId == Guid.Empty)
            {
                name = UserRoles.Anonymous;
                email = "anonymous@vaultmind.local";
                role = UserRoles.Anonymous;
            }
            else
            {
                var user = await _usersRepo.GetByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { Error = "User no longer exists" });
                }
                name = user.Name;
                email = user.Email;
                role = UserRoles.User;
            }

            // Generate new token pair
            var newAccessToken = _jwtService.GenerateAccessToken(userId, name, email, role);
            var newRefreshTokenString = _jwtService.GenerateRefreshToken();

            // Extract JTI (Jwt ID)
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(newAccessToken);
            var jti = jwtToken.Id;

            // Save new refresh token
            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenString,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
                IsRevoked = false
            };
            await _refreshTokensRepo.InsertOneAsync(newRefreshToken);

            // Save active access token session
            var activeAccessToken = new ActiveAccessToken
            {
                JwtId = jti,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes)
            };
            await _activeTokensRepo.InsertOneAsync(activeAccessToken);

            var expUnix = new DateTimeOffset(DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes)).ToUnixTimeSeconds();

            return Ok(new TokenResponse(newAccessToken, newRefreshTokenString, expUnix, expUnix));
        }

        // ── Anonymous Token Flow ──
        else
        {
            var anonymousUserId = Guid.Empty;
            var anonymousName = UserRoles.Anonymous;
            var anonymousEmail = "anonymous@vaultmind.local";
            var anonymousRole = UserRoles.Anonymous;

            var accessToken = _jwtService.GenerateAccessToken(anonymousUserId, anonymousName, anonymousEmail, anonymousRole);
            var refreshTokenString = _jwtService.GenerateRefreshToken();

            // Extract JTI (Jwt ID)
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(accessToken);
            var jti = jwtToken.Id;

            // Save refresh token
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = anonymousUserId,
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
                IsRevoked = false
            };
            await _refreshTokensRepo.InsertOneAsync(refreshToken);

            // Save active access token session
            var activeAccessToken = new ActiveAccessToken
            {
                JwtId = jti,
                UserId = anonymousUserId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes)
            };
            await _activeTokensRepo.InsertOneAsync(activeAccessToken);

            var expUnix = new DateTimeOffset(DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes)).ToUnixTimeSeconds();

            return Ok(new TokenResponse(accessToken, refreshTokenString, expUnix, expUnix));
        }
    }

    [HttpPost("signup")]
    [Authorize(Roles = UserRoles.Anonymous)]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { Error = "Email, Password, and Name are required" });
        }

        var existingUser = await _usersRepo.FindOneAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { Error = "Email already exists" });
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = passwordHash,
            Name = request.Name
        };

        await _usersRepo.InsertOneAsync(newUser);

        // Auto sign-in after sign-up
        var refreshTokenExpiryDays = double.Parse(_config["Auth:RefreshTokenExpirationDays"] ?? "7");
        var accessTokenExpiryMinutes = double.Parse(_config["Auth:AccessTokenExpirationMinutes"] ?? "60");

        var accessToken = _jwtService.GenerateAccessToken(newUser.Id, newUser.Name, newUser.Email, UserRoles.Anonymous);
        var refreshTokenString = _jwtService.GenerateRefreshToken();

        // Extract JTI
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(accessToken);
        var jti = jwtToken.Id;

        // Save refresh token
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = newUser.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            IsRevoked = false
        };
        await _refreshTokensRepo.InsertOneAsync(refreshToken);

        // Save active access token session
        var activeAccessToken = new ActiveAccessToken
        {
            JwtId = jti,
            UserId = newUser.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes)
        };
        await _activeTokensRepo.InsertOneAsync(activeAccessToken);

        var expUnix = new DateTimeOffset(DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes)).ToUnixTimeSeconds();

        return Ok(new TokenResponse(accessToken, refreshTokenString, expUnix, expUnix));
    }

    [HttpPost("signin")]
    [Authorize]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { Error = "Email and Password are required" });
        }

        // ── Verify User Credentials ──
        var user = await _usersRepo.FindOneAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Error = "Invalid email or password" });
        }

        //// ── Retrieve & Validate Anonymous Token ──
        //string? anonymousToken = request.AnonymousToken;

        //// If not in body, try to extract from Authorization header
        //if (string.IsNullOrEmpty(anonymousToken))
        //{
        //    var authHeader = Request.Headers.Authorization.ToString();
        //    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        //    {
        //        anonymousToken = authHeader.Substring("Bearer ".Length).Trim();
        //    }
        //}

        //if (string.IsNullOrEmpty(anonymousToken))
        //{
        //    return BadRequest(new { Error = "Anonymous token is required for sign-in migration" });
        //}

        //var principal = _jwtService.ValidateToken(anonymousToken);
        //if (principal == null)
        //{
        //    return BadRequest(new { Error = "Invalid or expired anonymous token" });
        //}

        //var nameClaim = principal.FindFirst("uniquename")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        //if (nameClaim != "anonymous")
        //{
        //    return BadRequest(new { Error = "Provided token is not an anonymous token" });
        //}

        // ── Revoke/Clean Up Anonymous Refresh Tokens ──
        // (Clean up any active anonymous sessions associated with this context)
        await _refreshTokensRepo.UpdateManyAsync(
            t => t.UserId == Guid.Empty && !t.IsRevoked,
            Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true).Set(t => t.ExpiresAt, DateTime.UtcNow)
        );

        // ── Generate Real User Token Pair ──
        var refreshTokenExpiryDays = double.Parse(_config["Auth:RefreshTokenExpirationDays"] ?? "7");
        var accessTokenExpiryMinutes = double.Parse(_config["Auth:AccessTokenExpirationMinutes"] ?? "60");

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Name, user.Email, UserRoles.Anonymous);
        var refreshTokenString = _jwtService.GenerateRefreshToken();

        // Extract JTI
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(accessToken);
        var jti = jwtToken.Id;

        // Save new refresh token
        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            IsRevoked = false
        };
        await _refreshTokensRepo.InsertOneAsync(refreshToken);

        // Save active access token session
        var activeAccessToken = new ActiveAccessToken
        {
            JwtId = jti,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes)
        };
        await _activeTokensRepo.InsertOneAsync(activeAccessToken);

        var expUnix = new DateTimeOffset(DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes)).ToUnixTimeSeconds();

        return Ok(new TokenResponse(accessToken, refreshTokenString, expUnix, expUnix));
    }
}

// ── Auth API Models ──
public record TokenRequest(Guid? RefreshToken);
public record SignUpRequest(string Email, string Password, string Name);
public record SignInRequest(string Email, string Password, string? AnonymousToken);

public record TokenResponse(
    string AccessToken,
    Guid RefreshToken,
    long ExpiresAt,
    long Exp
);

public record UserResponse(
    Guid Id,
    string Email,
    string Name
);

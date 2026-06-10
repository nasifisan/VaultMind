using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    private readonly IMongoRepository<Conversation> _conversationsRepo;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _config;

    public AuthController(
        IMongoRepository<User> usersRepo,
        IMongoRepository<RefreshToken> refreshTokensRepo,
        IMongoRepository<ActiveAccessToken> activeTokensRepo,
        IMongoRepository<Conversation> conversationsRepo,
        IJwtService jwtService,
        IConfiguration config)
    {
        _usersRepo = usersRepo;
        _refreshTokensRepo = refreshTokensRepo;
        _activeTokensRepo = activeTokensRepo;
        _conversationsRepo = conversationsRepo;
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

            var user = await _usersRepo.GetByIdAsync(userId);
            if (user == null)
            {
                // Unrecognized userId in Users collection: this is a unique guest/anonymous user
                name = UserRoles.Anonymous;
                email = "anonymous@vaultmind.local";
                role = UserRoles.Anonymous;
            }
            else
            {
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

        // ── Anonymous Token Flow (Initial Load) ──
        else
        {
            var anonymousUserId = Guid.NewGuid(); // Generate a unique Guid for this guest session
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

        var accessToken = _jwtService.GenerateAccessToken(newUser.Id, newUser.Name, newUser.Email, UserRoles.User);
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

        // ── Retrieve & Validate Anonymous Token and migrate sessions ──
        Guid? anonymousUserId = null;
        //string? anonymousToken = request.AnonymousToken;

        // If not in body, try to extract from Authorization header
        //if (string.IsNullOrEmpty(anonymousToken))
        //{
        //    var authHeader = Request.Headers.Authorization.ToString();
        //    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        //    {
        //        anonymousToken = authHeader.Substring("Bearer ".Length).Trim();
        //    }
        //}

        //if (!string.IsNullOrEmpty(anonymousToken))
        //{
        //    var principal = _jwtService.ValidateToken(anonymousToken);
        //    if (principal != null)
        //    {
        //        var nameClaim = principal.FindFirst("uniquename")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;

        //        // If it was indeed an anonymous token, extract the unique ID
        //        if (nameClaim == UserRoles.Anonymous)
        //        {
        //            var anonymousUserIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //            if (Guid.TryParse(anonymousUserIdClaim, out var parsedId))
        //            {
        //                anonymousUserId = parsedId;
        //            }
        //        }
        //    }
        //}

        var nameClaim = User.FindFirst("uniquename")?.Value ?? User.FindFirst(ClaimTypes.Name)?.Value;

        // Ensure the current token is indeed an anonymous session before migrating
        if (nameClaim == UserRoles.Anonymous)
        {
            var anonymousUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(anonymousUserIdClaim, out var parsedId))
            {
                anonymousUserId = parsedId;
            }
        }

        // ── Revoke/Clean Up Anonymous Refresh Tokens & Migrate Conversations ──
        if (anonymousUserId.HasValue && anonymousUserId.Value != Guid.Empty)
        {
            await _refreshTokensRepo.UpdateManyAsync(
                t => t.UserId == anonymousUserId.Value && !t.IsRevoked,
                Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true).Set(t => t.ExpiresAt, DateTime.UtcNow)
            );

            await _conversationsRepo.UpdateManyAsync(
                c => c.UserId == anonymousUserId.Value,
                Builders<Conversation>.Update.Set(c => c.UserId, user.Id)
            );
        }

        // ── Generate Real User Token Pair ──
        var refreshTokenExpiryDays = double.Parse(_config["Auth:RefreshTokenExpirationDays"] ?? "7");
        var accessTokenExpiryMinutes = double.Parse(_config["Auth:AccessTokenExpirationMinutes"] ?? "60");

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Name, user.Email, UserRoles.User);
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
public record SignInRequest(string Email, string Password);

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

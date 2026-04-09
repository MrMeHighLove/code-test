using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using ProductsApi.Configuration;
using ProductsApi.Models;

namespace ProductsApi.Services;

public class AuthService(
    IMongoDbContext context,
    IOptions<JwtSettings> jwtSettings,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();

        using var existingCursor = await context.Users.FindAsync(
            u => u.Username == username,
            cancellationToken: cancellationToken);

        var existingUser = await existingCursor.FirstOrDefaultAsync(cancellationToken);
        if (existingUser is not null)
        {
            logger.LogWarning("Registration rejected for duplicate username {Username}.", username);
            return (false, "Username already exists.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
            logger.LogInformation("Registered user {Username}.", username);
            return (true, "User registered successfully.");
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            logger.LogWarning("Registration raced with another request for username {Username}.", username);
            return (false, "Username already exists.");
        }
    }

    public async Task<(bool Success, string AccessToken, string RefreshToken, string Message)> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();

        using var userCursor = await context.Users.FindAsync(
            u => u.Username == username,
            cancellationToken: cancellationToken);

        var user = await userCursor.FirstOrDefaultAsync(cancellationToken);
        if (user is null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for username {Username}.", username);
            return (false, string.Empty, string.Empty, "Invalid username or password.");
        }

        var accessToken = GenerateAccessToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id!, cancellationToken);
        logger.LogInformation("Login succeeded for user {UserId}.", user.Id);

        return (true, accessToken, refreshToken, "Login successful.");
    }

    public async Task<(bool Success, string AccessToken, string NewRefreshToken, string Message)> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        using var refreshCursor = await context.RefreshTokens.FindAsync(
            rt => rt.Token == refreshToken,
            cancellationToken: cancellationToken);

        var stored = await refreshCursor.FirstOrDefaultAsync(cancellationToken);
        if (stored is null || stored.ExpiresAt < DateTime.UtcNow)
        {
            logger.LogWarning("Refresh token rejected.");
            return (false, string.Empty, string.Empty, "Invalid or expired refresh token.");
        }

        await context.RefreshTokens.DeleteOneAsync(rt => rt.Id == stored.Id, cancellationToken);

        using var userCursor = await context.Users.FindAsync(
            u => u.Id == stored.UserId,
            cancellationToken: cancellationToken);

        var user = await userCursor.FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Refresh token belonged to a missing user {UserId}.", stored.UserId);
            return (false, string.Empty, string.Empty, "User not found.");
        }

        var newAccessToken = GenerateAccessToken(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id!, cancellationToken);
        logger.LogInformation("Refresh token rotated for user {UserId}.", user.Id);

        return (true, newAccessToken, newRefreshToken, "Token refreshed.");
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        await context.RefreshTokens.DeleteOneAsync(rt => rt.Token == refreshToken, cancellationToken);
        logger.LogInformation("Refresh token invalidated during logout.");
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        await context.RefreshTokens.InsertOneAsync(
            new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            },
            cancellationToken: cancellationToken);

        return token;
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);
        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }
}

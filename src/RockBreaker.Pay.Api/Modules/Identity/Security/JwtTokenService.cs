using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Identity.Abstractions;
using RockBreaker.Pay.Modules.Identity.Contracts;
using RockBreaker.Pay.Modules.Identity.Domain;

namespace RockBreaker.Pay.Modules.Identity.Security;

/// <summary>
/// TR: HMAC-SHA256 JWT access token ve hash'lenmiş, rotate edilen refresh token üretir.
/// EN: Creates HMAC-SHA256 JWT access tokens and hashed, rotated refresh tokens.
/// Architecture: JWT Authentication + Refresh Token Rotation.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUserRepository _userRepository;

    /// <summary>TR: Token servisi bağımlılıklarını alır. EN: Receives token-service dependencies. Architecture: Constructor Injection.</summary>
    /// <param name="configuration">TR: Konfigürasyon. EN: Configuration.</param>
    /// <param name="connectionFactory">TR: DB factory. EN: DB factory.</param>
    /// <param name="userRepository">TR: User repository. EN: User repository.</param>
    public JwtTokenService(
        IConfiguration configuration,
        IDbConnectionFactory connectionFactory,
        IUserRepository userRepository)
    {
        _configuration = configuration;
        _connectionFactory = connectionFactory;
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<TokenResponse> CreateAsync(UserAccount user)
    {
        var key = GetKey();
        var expires = DateTime.UtcNow.AddMinutes(15);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            ],
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var refreshHash = HashToken(refreshToken);

        const string sql = """
            INSERT INTO dbo.RefreshTokens(Id, UserId, TokenHash, ExpiresAtUtc, CreatedAtUtc, IsRevoked)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAtUtc, SYSUTCDATETIME(), 0);
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        });

        return new TokenResponse { AccessToken = accessToken, RefreshToken = refreshToken, ExpiresAtUtc = expires };
    }

    /// <inheritdoc />
    public async Task<TokenResponse?> RefreshAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        const string sql = """
            SELECT TOP 1 Id, UserId
            FROM dbo.RefreshTokens
            WHERE TokenHash = @TokenHash
              AND IsRevoked = 0
              AND ExpiresAtUtc > SYSUTCDATETIME();
            """;

        using var connection = _connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(sql, new { TokenHash = tokenHash });
        if (row is null)
        {
            return null;
        }

        await connection.ExecuteAsync(
            "UPDATE dbo.RefreshTokens SET IsRevoked = 1, RevokedAtUtc = SYSUTCDATETIME() WHERE Id = @Id;",
            new { row.Id });

        var user = await _userRepository.GetByIdAsync(row.UserId);
        return user is null ? null : await CreateAsync(user);
    }

    private SymmetricSecurityKey GetKey()
    {
        var secret = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is required.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed class RefreshTokenRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
    }
}

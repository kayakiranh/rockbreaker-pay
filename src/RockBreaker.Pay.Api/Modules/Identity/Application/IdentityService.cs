using System.Security.Cryptography;
using System.Text;
using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Identity.Abstractions;
using RockBreaker.Pay.Modules.Identity.Contracts;
using RockBreaker.Pay.Modules.Identity.Domain;
using RockBreaker.Pay.Modules.Identity.Mapping;
using RockBreaker.Pay.Modules.Identity.Security;

namespace RockBreaker.Pay.Modules.Identity.Application;

/// <summary>
/// TR: Identity ve profil use-case'lerini basit application service olarak orkestre eder.
/// EN: Orchestrates identity and profile use cases as a simple application service.
/// Architecture: Application Service + Repository + Explicit Mapper.
/// </summary>
public sealed class IdentityService : IIdentityService
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _tokenService;
    private readonly IProfileMapper _profileMapper;
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>TR: Identity service bağımlılıklarını alır. EN: Receives identity-service dependencies. Architecture: Constructor Injection.</summary>
    public IdentityService(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        IJwtTokenService tokenService,
        IProfileMapper profileMapper,
        IDbConnectionFactory connectionFactory)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _profileMapper = profileMapper;
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<OperationResult<TokenResponse>> RegisterAsync(RegisterRequest request)
    {
        if (await _repository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant()) is not null)
            return OperationResult<TokenResponse>.Fail("EMAIL_EXISTS", "E-mail is already registered.");

        if (request.Password.Length < 8)
            return OperationResult<TokenResponse>.Fail("WEAK_PASSWORD", "Password must contain at least 8 characters.");

        var now = DateTime.UtcNow;
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _repository.InsertAsync(user);
        return OperationResult<TokenResponse>.Success(await _tokenService.CreateAsync(user));
    }

    /// <inheritdoc />
    public async Task<OperationResult<TokenResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _repository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return OperationResult<TokenResponse>.Fail("INVALID_CREDENTIALS", "E-mail or password is invalid.");

        return OperationResult<TokenResponse>.Success(await _tokenService.CreateAsync(user));
    }

    /// <inheritdoc />
    public async Task<OperationResult<TokenResponse>> RefreshAsync(RefreshTokenRequest request)
    {
        var token = await _tokenService.RefreshAsync(request.RefreshToken);
        return token is null
            ? OperationResult<TokenResponse>.Fail("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.")
            : OperationResult<TokenResponse>.Success(token);
    }

    /// <inheritdoc />
    public async Task<OperationResult<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _repository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null)
            return OperationResult<string>.Success("If the account exists, a reset token was created.");

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        const string sql = """
            INSERT INTO dbo.PasswordResetTokens(Id, UserId, TokenHash, ExpiresAtUtc, CreatedAtUtc, IsUsed)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAtUtc, SYSUTCDATETIME(), 0);
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
        });

        return OperationResult<string>.Success(rawToken);
    }

    /// <inheritdoc />
    public async Task<OperationResult<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword.Length < 8)
            return OperationResult<bool>.Fail("WEAK_PASSWORD", "Password must contain at least 8 characters.");

        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        const string sql = """
            SELECT TOP 1 Id, UserId
            FROM dbo.PasswordResetTokens
            WHERE TokenHash = @TokenHash AND IsUsed = 0 AND ExpiresAtUtc > SYSUTCDATETIME();
            """;

        using var connection = _connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<ResetTokenRow>(sql, new { TokenHash = tokenHash });
        if (row is null)
            return OperationResult<bool>.Fail("INVALID_RESET_TOKEN", "Reset token is invalid or expired.");

        await _repository.UpdatePasswordAsync(row.UserId, _passwordHasher.Hash(request.NewPassword));
        await connection.ExecuteAsync(
            "UPDATE dbo.PasswordResetTokens SET IsUsed = 1, UsedAtUtc = SYSUTCDATETIME() WHERE Id = @Id;",
            new { row.Id });

        return OperationResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<OperationResult<ProfileResponse>> GetProfileAsync(Guid userId)
    {
        var user = await _repository.GetByIdAsync(userId);
        return user is null
            ? OperationResult<ProfileResponse>.Fail("USER_NOT_FOUND", "User was not found.")
            : OperationResult<ProfileResponse>.Success(_profileMapper.Map(user));
    }

    /// <inheritdoc />
    public async Task<OperationResult<ProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _repository.GetByIdAsync(userId);
        if (user is null)
            return OperationResult<ProfileResponse>.Fail("USER_NOT_FOUND", "User was not found.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Phone = request.Phone;
        user.SmsEnabled = request.SmsEnabled;
        user.EmailEnabled = request.EmailEnabled;
        user.PushEnabled = request.PushEnabled;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _repository.UpdateProfileAsync(user);
        return OperationResult<ProfileResponse>.Success(_profileMapper.Map(user));
    }

    private sealed class ResetTokenRow
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
    }
}

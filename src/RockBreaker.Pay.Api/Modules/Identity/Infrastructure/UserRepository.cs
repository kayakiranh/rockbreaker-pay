using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Identity.Abstractions;
using RockBreaker.Pay.Modules.Identity.Domain;

namespace RockBreaker.Pay.Modules.Identity.Infrastructure;

/// <summary>
/// TR: Kullanıcı verisini Dapper ile MSSQL üzerinde yönetir.
/// EN: Manages user data on MSSQL using Dapper.
/// Architecture: Repository Pattern.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>TR: Repository bağımlılıklarını alır. EN: Receives repository dependencies. Architecture: Constructor Injection.</summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    public UserRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<UserAccount?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM dbo.Users WHERE Email = @Email;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<UserAccount>(sql, new { Email = email });
    }

    /// <inheritdoc />
    public async Task<UserAccount?> GetByIdAsync(Guid userId)
    {
        const string sql = "SELECT * FROM dbo.Users WHERE Id = @UserId;";
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<UserAccount>(sql, new { UserId = userId });
    }

    /// <inheritdoc />
    public async Task InsertAsync(UserAccount user)
    {
        const string sql = """
            INSERT INTO dbo.Users
                (Id, Email, PasswordHash, FirstName, LastName, Phone, Role, KycStatus,
                 SmsEnabled, EmailEnabled, PushEnabled, CreatedAtUtc, UpdatedAtUtc)
            VALUES
                (@Id, @Email, @PasswordHash, @FirstName, @LastName, @Phone, @Role, @KycStatus,
                 @SmsEnabled, @EmailEnabled, @PushEnabled, @CreatedAtUtc, @UpdatedAtUtc);
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, user);
    }

    /// <inheritdoc />
    public async Task UpdateProfileAsync(UserAccount user)
    {
        const string sql = """
            UPDATE dbo.Users
            SET FirstName = @FirstName, LastName = @LastName, Phone = @Phone,
                SmsEnabled = @SmsEnabled, EmailEnabled = @EmailEnabled, PushEnabled = @PushEnabled,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE Id = @Id;
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, user);
    }

    /// <inheritdoc />
    public async Task UpdatePasswordAsync(Guid userId, string passwordHash)
    {
        const string sql = """
            UPDATE dbo.Users
            SET PasswordHash = @PasswordHash, UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = @UserId;
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { UserId = userId, PasswordHash = passwordHash });
    }

    /// <inheritdoc />
    public async Task UpdateKycStatusAsync(Guid userId, string kycStatus)
    {
        const string sql = """
            UPDATE dbo.Users
            SET KycStatus = @KycStatus,
                UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = @UserId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            sql,
            new { UserId = userId, KycStatus = kycStatus });
    }
}

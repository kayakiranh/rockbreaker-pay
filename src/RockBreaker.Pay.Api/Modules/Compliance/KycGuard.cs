using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;

namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: Users/Wallets read model üzerinden KYC uygunluğunu kontrol eder.
/// EN: Checks KYC eligibility through the Users/Wallets read model.
/// Architecture: Compliance Policy + Dapper Read Model.
/// </summary>
public sealed class KycGuard : IKycGuard
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// TR: KYC guard bağımlılıklarını alır.
    /// EN: Receives KYC guard dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public KycGuard(IDbConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<bool> IsUserVerifiedAsync(Guid userId)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.Users
            WHERE Id = @UserId
              AND KycStatus = @Verified;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(
            sql,
            new { UserId = userId, Verified = KycStates.Verified }) == 1;
    }

    /// <inheritdoc />
    public async Task<bool> IsWalletOwnerVerifiedAsync(Guid walletId)
    {
        const string sql = """
            SELECT COUNT_BIG(1)
            FROM dbo.Wallets w
            INNER JOIN dbo.Users u ON u.Id = w.UserId
            WHERE w.Id = @WalletId
              AND u.KycStatus = @Verified;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(
            sql,
            new { WalletId = walletId, Verified = KycStates.Verified }) == 1;
    }
}

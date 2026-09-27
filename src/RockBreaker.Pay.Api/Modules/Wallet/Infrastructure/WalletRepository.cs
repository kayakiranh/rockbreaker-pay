using System.Data;
using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Domain;

namespace RockBreaker.Pay.Modules.Wallet.Infrastructure;

/// <summary>
/// TR: Wallet SQL işlemlerini Dapper ile uygular.
/// EN: Implements wallet SQL operations with Dapper.
/// Architecture: Repository Pattern.
/// </summary>
public sealed class WalletRepository : IWalletRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// TR: Repository bağımlılıklarını alır.
    /// EN: Receives repository dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    public WalletRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<Wallet?> GetByIdAsync(Guid walletId)
    {
        const string sql = """
            SELECT Id, UserId, Balance, Currency, Status, SingleTransactionLimit,
                   DailyLimit, MonthlyLimit, CreatedAtUtc, UpdatedAtUtc
            FROM dbo.Wallets
            WHERE Id = @WalletId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Wallet>(sql, new { WalletId = walletId });
    }

    /// <inheritdoc />
    public async Task<Wallet?> GetForUpdateAsync(
        Guid walletId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        const string sql = """
            SELECT Id, UserId, Balance, Currency, Status, SingleTransactionLimit,
                   DailyLimit, MonthlyLimit, CreatedAtUtc, UpdatedAtUtc
            FROM dbo.Wallets WITH (UPDLOCK, ROWLOCK)
            WHERE Id = @WalletId;
            """;

        return await connection.QuerySingleOrDefaultAsync<Wallet>(
            sql,
            new { WalletId = walletId },
            transaction);
    }

    /// <inheritdoc />
    public Task UpdateAsync(Wallet wallet, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = """
            UPDATE dbo.Wallets
            SET Balance = @Balance,
                Status = @Status,
                SingleTransactionLimit = @SingleTransactionLimit,
                DailyLimit = @DailyLimit,
                MonthlyLimit = @MonthlyLimit,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE Id = @Id;
            """;

        return connection.ExecuteAsync(sql, wallet, transaction);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(Guid walletId, WalletStatus status)
    {
        const string sql = """
            UPDATE dbo.Wallets
            SET Status = @Status, UpdatedAtUtc = SYSUTCDATETIME()
            WHERE Id = @WalletId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { WalletId = walletId, Status = (int)status });
    }
}

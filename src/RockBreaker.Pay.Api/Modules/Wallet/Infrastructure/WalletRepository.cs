using System.Data;
using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Domain;
using WalletEntity = RockBreaker.Pay.Modules.Wallet.Domain.Wallet;

namespace RockBreaker.Pay.Modules.Wallet.Infrastructure;

/// <summary>
/// TR: Wallet SQL işlemlerini Dapper ile uygular.
/// EN: Implements wallet SQL operations with Dapper.
/// Architecture: Repository Pattern.
/// </summary>
public sealed class WalletRepository : IWalletRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>TR: Repository bağımlılıklarını alır. EN: Receives repository dependencies. Architecture: Constructor Injection.</summary>
    public WalletRepository(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<WalletEntity?> GetByIdAsync(Guid walletId)
    {
        const string sql = """
            SELECT Id, UserId, Balance, Currency, Status, SingleTransactionLimit,
                   DailyLimit, MonthlyLimit, CreatedAtUtc, UpdatedAtUtc
            FROM dbo.Wallets WHERE Id = @WalletId;
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<WalletEntity>(sql, new { WalletId = walletId });
    }

    /// <inheritdoc />
    public async Task<WalletEntity?> GetByUserIdAsync(Guid userId)
    {
        const string sql = """
            SELECT Id, UserId, Balance, Currency, Status, SingleTransactionLimit,
                   DailyLimit, MonthlyLimit, CreatedAtUtc, UpdatedAtUtc
            FROM dbo.Wallets WHERE UserId = @UserId;
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<WalletEntity>(sql, new { UserId = userId });
    }

    /// <inheritdoc />
    public async Task InsertAsync(WalletEntity wallet)
    {
        const string sql = """
            INSERT INTO dbo.Wallets
                (Id, UserId, Balance, Currency, Status, SingleTransactionLimit,
                 DailyLimit, MonthlyLimit, CreatedAtUtc, UpdatedAtUtc)
            VALUES
                (@Id, @UserId, @Balance, @Currency, @Status, @SingleTransactionLimit,
                 @DailyLimit, @MonthlyLimit, @CreatedAtUtc, @UpdatedAtUtc);
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            wallet.Id,
            wallet.UserId,
            wallet.Balance,
            wallet.Currency,
            Status = (int)wallet.Status,
            wallet.SingleTransactionLimit,
            wallet.DailyLimit,
            wallet.MonthlyLimit,
            wallet.CreatedAtUtc,
            wallet.UpdatedAtUtc
        });
    }

    /// <inheritdoc />
    public async Task<WalletEntity?> GetForUpdateAsync(Guid walletId, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = """
            SELECT Id, UserId, Balance, Currency, Status, SingleTransactionLimit,
                   DailyLimit, MonthlyLimit, CreatedAtUtc, UpdatedAtUtc
            FROM dbo.Wallets WITH (UPDLOCK, ROWLOCK)
            WHERE Id = @WalletId;
            """;
        return await connection.QuerySingleOrDefaultAsync<WalletEntity>(sql, new { WalletId = walletId }, transaction);
    }

    /// <inheritdoc />
    public Task UpdateAsync(WalletEntity wallet, IDbConnection connection, IDbTransaction transaction)
    {
        const string sql = """
            UPDATE dbo.Wallets
            SET Balance = @Balance, Status = @Status,
                SingleTransactionLimit = @SingleTransactionLimit,
                DailyLimit = @DailyLimit, MonthlyLimit = @MonthlyLimit,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE Id = @Id;
            """;
        return connection.ExecuteAsync(sql, new
        {
            wallet.Id,
            wallet.Balance,
            Status = (int)wallet.Status,
            wallet.SingleTransactionLimit,
            wallet.DailyLimit,
            wallet.MonthlyLimit,
            wallet.UpdatedAtUtc
        }, transaction);
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

    /// <inheritdoc />
    public async Task UpdateLimitsAsync(WalletEntity wallet)
    {
        const string sql = """
            UPDATE dbo.Wallets
            SET SingleTransactionLimit = @SingleTransactionLimit,
                DailyLimit = @DailyLimit,
                MonthlyLimit = @MonthlyLimit,
                UpdatedAtUtc = @UpdatedAtUtc
            WHERE Id = @Id;
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, wallet);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<WalletTransaction>> GetTransactionsAsync(Guid walletId)
    {
        const string sql = """
            SELECT Id, SourceWalletId, DestinationWalletId, Amount, Currency, Type, Status,
                   IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc
            FROM dbo.WalletTransactions
            WHERE SourceWalletId = @WalletId OR DestinationWalletId = @WalletId
            ORDER BY CreatedAtUtc DESC;
            """;
        using var connection = _connectionFactory.CreateConnection();
        return (await connection.QueryAsync<WalletTransaction>(sql, new { WalletId = walletId })).ToArray();
    }
}

using System.Data;
using Dapper;
using RockBreaker.Pay.Modules.Identity.Security;
using RockBreaker.Pay.Modules.Wallet.Domain;

namespace RockBreaker.Pay.Infrastructure.Persistence;

/// <summary>
/// TR: Yalnız Development ortamında örnek Admin/Auditor/User hesapları ve ledger-consistent başlangıç bakiyeleri üretir.
/// EN: Creates sample Admin/Auditor/User accounts and ledger-consistent opening balances only in Development.
/// Architecture: Development Seed Data + Ledger Integrity.
/// </summary>
internal sealed class DevelopmentDataSeeder
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        IDbConnectionFactory connectionFactory,
        IPasswordHasher passwordHasher,
        IHostEnvironment environment,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _connectionFactory = connectionFactory;
        _passwordHasher = passwordHasher;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Demo seed data can only be enabled in the Development environment.");
        }

        var users = new[]
        {
            new SeedUser(
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Guid.Parse("20000000-0000-0000-0000-000000000001"),
                "admin@rockbreaker.local",
                "Admin123!",
                "RockBreaker",
                "Admin",
                "Admin",
                25_000m),
            new SeedUser(
                Guid.Parse("10000000-0000-0000-0000-000000000002"),
                Guid.Parse("20000000-0000-0000-0000-000000000002"),
                "auditor@rockbreaker.local",
                "Auditor123!",
                "RockBreaker",
                "Auditor",
                "Auditor",
                10_000m),
            new SeedUser(
                Guid.Parse("10000000-0000-0000-0000-000000000003"),
                Guid.Parse("20000000-0000-0000-0000-000000000003"),
                "alice@rockbreaker.local",
                "User123!",
                "Alice",
                "Wallet",
                "User",
                100_000m),
            new SeedUser(
                Guid.Parse("10000000-0000-0000-0000-000000000004"),
                Guid.Parse("20000000-0000-0000-0000-000000000004"),
                "bob@rockbreaker.local",
                "User123!",
                "Bob",
                "Wallet",
                "User",
                50_000m)
        };

        foreach (var user in users)
        {
            await SeedUserAsync(user);
        }
    }

    private async Task SeedUserAsync(SeedUser seed)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var userId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            """
            SELECT Id
            FROM dbo.Users
            WHERE Email = @Email;
            """,
            new { seed.Email },
            transaction);

        if (userId is null)
        {
            userId = seed.UserId;

            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.Users
                    (Id, Email, PasswordHash, FirstName, LastName, Phone, Role, KycStatus,
                     SmsEnabled, EmailEnabled, PushEnabled, CreatedAtUtc, UpdatedAtUtc)
                VALUES
                    (@Id, @Email, @PasswordHash, @FirstName, @LastName, NULL, @Role, 'Verified',
                     1, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                """,
                new
                {
                    Id = seed.UserId,
                    seed.Email,
                    PasswordHash = _passwordHasher.Hash(seed.Password),
                    seed.FirstName,
                    seed.LastName,
                    seed.Role
                },
                transaction);
        }

        var walletId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            """
            SELECT Id
            FROM dbo.Wallets
            WHERE UserId = @UserId;
            """,
            new { UserId = userId.Value },
            transaction);

        if (walletId is null)
        {
            walletId = seed.WalletId;

            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.Wallets
                    (Id, UserId, Balance, Currency, Status, SingleTransactionLimit,
                     DailyLimit, MonthlyLimit, CreatedAtUtc, UpdatedAtUtc)
                VALUES
                    (@Id, @UserId, @Balance, 'TRY', @Status, 100000, 250000, 1000000,
                     SYSUTCDATETIME(), SYSUTCDATETIME());
                """,
                new
                {
                    Id = seed.WalletId,
                    UserId = userId.Value,
                    Balance = seed.OpeningBalance,
                    Status = (int)WalletStatus.Active
                },
                transaction);

            if (seed.OpeningBalance > 0)
            {
                await InsertOpeningBalanceAsync(
                    connection,
                    transaction,
                    seed.WalletId,
                    seed.OpeningBalance);
            }
        }

        transaction.Commit();

        _logger.LogInformation(
            "Development seed account ready: {Email} ({Role}).",
            seed.Email,
            seed.Role);
    }

    private static async Task InsertOpeningBalanceAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid walletId,
        decimal amount)
    {
        var transactionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.WalletTransactions
                (Id, SourceWalletId, DestinationWalletId, Amount, Currency, Type, Status,
                 IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc)
            VALUES
                (@Id, NULL, @WalletId, @Amount, 'TRY', 'OpeningBalance', @Status,
                 @IdempotencyKey, @CorrelationId, @CreatedAtUtc, @CompletedAtUtc);
            """,
            new
            {
                Id = transactionId,
                WalletId = walletId,
                Amount = amount,
                Status = (int)TransactionStatus.Completed,
                IdempotencyKey = $"seed:opening:{walletId}",
                CorrelationId = $"seed:{walletId}",
                CreatedAtUtc = now,
                CompletedAtUtc = now
            },
            transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.LedgerEntries
                (Id, TransactionId, WalletId, Direction, Amount, BalanceAfter, Currency, CreatedAtUtc)
            VALUES
                (@Id, @TransactionId, @WalletId, @Direction, @Amount, @BalanceAfter, 'TRY', @CreatedAtUtc);
            """,
            new
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                WalletId = walletId,
                Direction = (int)LedgerDirection.Credit,
                Amount = amount,
                BalanceAfter = amount,
                CreatedAtUtc = now
            },
            transaction);
    }

    private sealed record SeedUser(
        Guid UserId,
        Guid WalletId,
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role,
        decimal OpeningBalance);
}

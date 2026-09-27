using System.Data;
using System.Text.Json;
using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Contracts;
using RockBreaker.Pay.Modules.Wallet.Domain;

namespace RockBreaker.Pay.Modules.Wallet.Infrastructure;

/// <summary>
/// TR: Wallet transferini MSSQL üzerinde atomik olarak gerçekleştirir.
/// EN: Executes a wallet transfer atomically on MSSQL.
/// Architecture: Unit of Work + Pessimistic Locking + Double-Entry Ledger + Transactional Outbox.
/// </summary>
public sealed class WalletTransferStore : IWalletTransferStore
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletLimitGuard _limitGuard;

    /// <summary>
    /// TR: Store bağımlılıklarını alır.
    /// EN: Receives store dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    /// <param name="walletRepository">TR: Wallet repository. EN: Wallet repository.</param>
    public WalletTransferStore(
        IDbConnectionFactory connectionFactory,
        IWalletRepository walletRepository,
        IWalletLimitGuard limitGuard)
    {
        _connectionFactory = connectionFactory;
        _walletRepository = walletRepository;
        _limitGuard = limitGuard;
    }

    /// <inheritdoc />
    public async Task<OperationResult<TransferResponse>> ExecuteAsync(
        TransferRequest request,
        string idempotencyKey,
        string correlationId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        const string existingSql = """
            SELECT Id, SourceWalletId, DestinationWalletId, Amount, Currency, Type, Status,
                   IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc
            FROM dbo.WalletTransactions
            WHERE IdempotencyKey = @IdempotencyKey;
            """;

        var existing = await connection.QuerySingleOrDefaultAsync<WalletTransaction>(
            existingSql,
            new { IdempotencyKey = idempotencyKey },
            transaction);

        if (existing is not null)
        {
            transaction.Rollback();
            return OperationResult<TransferResponse>.Fail(
                "DUPLICATE_REQUEST",
                $"A transaction already exists for idempotency key {idempotencyKey}.");
        }

        var orderedIds = new[] { request.SourceWalletId, request.DestinationWalletId }.OrderBy(x => x).ToArray();
        var first = await _walletRepository.GetForUpdateAsync(orderedIds[0], connection, transaction);
        var second = await _walletRepository.GetForUpdateAsync(orderedIds[1], connection, transaction);

        var source = first?.Id == request.SourceWalletId ? first : second;
        var destination = first?.Id == request.DestinationWalletId ? first : second;

        if (source is null || destination is null)
        {
            transaction.Rollback();
            return OperationResult<TransferResponse>.Fail("WALLET_NOT_FOUND", "Source or destination wallet was not found.");
        }

        if (source.Status != WalletStatus.Active || destination.Status != WalletStatus.Active)
        {
            transaction.Rollback();
            return OperationResult<TransferResponse>.Fail("WALLET_NOT_ACTIVE", "Source and destination wallets must be active.");
        }

        if (source.Currency != request.Currency || destination.Currency != request.Currency)
        {
            transaction.Rollback();
            return OperationResult<TransferResponse>.Fail("CURRENCY_MISMATCH", "Wallet and transaction currencies must match.");
        }

        var limitResult = await _limitGuard.CheckAsync(source, request.Amount, connection, transaction);
        if (!limitResult.IsAllowed)
        {
            transaction.Rollback();
            return OperationResult<TransferResponse>.Fail(
                limitResult.ErrorCode!,
                limitResult.ErrorMessage!);
        }

        if (source.Balance < request.Amount)
        {
            transaction.Rollback();
            return OperationResult<TransferResponse>.Fail("INSUFFICIENT_BALANCE", "Wallet balance is insufficient.");
        }

        var now = DateTime.UtcNow;
        var transactionId = Guid.NewGuid();

        source.Balance -= request.Amount;
        source.UpdatedAtUtc = now;
        destination.Balance += request.Amount;
        destination.UpdatedAtUtc = now;

        await _walletRepository.UpdateAsync(source, connection, transaction);
        await _walletRepository.UpdateAsync(destination, connection, transaction);

        const string insertTransactionSql = """
            INSERT INTO dbo.WalletTransactions
                (Id, SourceWalletId, DestinationWalletId, Amount, Currency, Type, Status,
                 IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc)
            VALUES
                (@Id, @SourceWalletId, @DestinationWalletId, @Amount, @Currency, @Type, @Status,
                 @IdempotencyKey, @CorrelationId, @CreatedAtUtc, @CompletedAtUtc);
            """;

        await connection.ExecuteAsync(insertTransactionSql, new
        {
            Id = transactionId,
            request.SourceWalletId,
            request.DestinationWalletId,
            request.Amount,
            request.Currency,
            Type = "WalletToWallet",
            Status = (int)TransactionStatus.Completed,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId,
            CreatedAtUtc = now,
            CompletedAtUtc = now
        }, transaction);

        const string insertLedgerSql = """
            INSERT INTO dbo.LedgerEntries
                (Id, TransactionId, WalletId, Direction, Amount, BalanceAfter, Currency, CreatedAtUtc)
            VALUES
                (@Id, @TransactionId, @WalletId, @Direction, @Amount, @BalanceAfter, @Currency, @CreatedAtUtc);
            """;

        await connection.ExecuteAsync(insertLedgerSql, new[]
        {
            new
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                WalletId = source.Id,
                Direction = (int)LedgerDirection.Debit,
                request.Amount,
                BalanceAfter = source.Balance,
                request.Currency,
                CreatedAtUtc = now
            },
            new
            {
                Id = Guid.NewGuid(),
                TransactionId = transactionId,
                WalletId = destination.Id,
                Direction = (int)LedgerDirection.Credit,
                request.Amount,
                BalanceAfter = destination.Balance,
                request.Currency,
                CreatedAtUtc = now
            }
        }, transaction);

        const string insertOutboxSql = """
            INSERT INTO dbo.OutboxMessages
                (Id, Type, Payload, CreatedAtUtc, RetryCount)
            VALUES
                (@Id, @Type, @Payload, @CreatedAtUtc, 0);
            """;

        await connection.ExecuteAsync(insertOutboxSql, new
        {
            Id = Guid.NewGuid(),
            Type = "WalletTransferCompleted",
            Payload = JsonSerializer.Serialize(new
            {
                TransactionId = transactionId,
                request.SourceWalletId,
                request.DestinationWalletId,
                request.Amount,
                request.Currency,
                CorrelationId = correlationId
            }),
            CreatedAtUtc = now
        }, transaction);

        transaction.Commit();

        return OperationResult<TransferResponse>.Success(new TransferResponse
        {
            TransactionId = transactionId,
            SourceBalance = source.Balance,
            DestinationBalance = destination.Balance,
            Status = TransactionStatus.Completed.ToString()
        });
    }
}

using System.Data;
using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Domain;
using RockBreaker.Pay.Modules.Wallet.Application;

namespace RockBreaker.Pay.Modules.Government;

/// <summary>
/// TR: Fatura ödeme wallet hareketlerini MSSQL transaction ve ledger kayıtlarıyla uygular.
/// EN: Applies wallet movements for bill payments using MSSQL transactions and ledger records.
/// Architecture: Unit of Work + Pessimistic Locking + Ledger + Compensation.
/// </summary>
public sealed class BillPaymentStore : IBillPaymentStore
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletLimitGuard _limitGuard;

    /// <summary>
    /// TR: Store bağımlılıklarını alır.
    /// EN: Receives store dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public BillPaymentStore(
        IDbConnectionFactory connectionFactory,
        IWalletRepository walletRepository,
        IWalletLimitGuard limitGuard)
    {
        _connectionFactory = connectionFactory;
        _walletRepository = walletRepository;
        _limitGuard = limitGuard;
    }

    /// <inheritdoc />
    public async Task<OperationResult<BillPaymentResponse>> DebitAsync(
        Guid walletId,
        Guid billId,
        decimal amount,
        string idempotencyKey,
        string correlationId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        const string duplicateSql =
            "SELECT COUNT_BIG(1) FROM dbo.WalletTransactions WHERE IdempotencyKey = @IdempotencyKey;";

        if (await connection.ExecuteScalarAsync<long>(
                duplicateSql,
                new { IdempotencyKey = idempotencyKey },
                transaction) > 0)
        {
            transaction.Rollback();
            return OperationResult<BillPaymentResponse>.Fail(
                "DUPLICATE_REQUEST",
                "Idempotency key already exists.");
        }

        var wallet = await _walletRepository.GetForUpdateAsync(walletId, connection, transaction);
        if (wallet is null || wallet.Status != WalletStatus.Active)
        {
            transaction.Rollback();
            return OperationResult<BillPaymentResponse>.Fail(
                "WALLET_NOT_ACTIVE",
                "Wallet was not found or is not active.");
        }

        if (wallet.Balance < amount)
        {
            transaction.Rollback();
            return OperationResult<BillPaymentResponse>.Fail(
                "INSUFFICIENT_BALANCE",
                "Wallet balance is insufficient.");
        }

        var limitResult = await _limitGuard.CheckAsync(wallet, amount, connection, transaction);
        if (!limitResult.IsAllowed)
        {
            transaction.Rollback();
            return OperationResult<BillPaymentResponse>.Fail(
                limitResult.ErrorCode!,
                limitResult.ErrorMessage!);
        }

        wallet.Balance -= amount;
        wallet.UpdatedAtUtc = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, connection, transaction);

        var transactionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        const string txSql = """
            INSERT INTO dbo.WalletTransactions
                (Id, SourceWalletId, DestinationWalletId, Amount, Currency, Type, Status,
                 IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc)
            VALUES
                (@Id, @WalletId, NULL, @Amount, 'TRY', 'BillPayment', @Status,
                 @IdempotencyKey, @CorrelationId, @CreatedAtUtc, @CompletedAtUtc);
            """;

        await connection.ExecuteAsync(txSql, new
        {
            Id = transactionId,
            WalletId = walletId,
            Amount = amount,
            Status = (int)TransactionStatus.Completed,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId,
            CreatedAtUtc = now,
            CompletedAtUtc = now
        }, transaction);

        const string ledgerSql = """
            INSERT INTO dbo.LedgerEntries
                (Id, TransactionId, WalletId, Direction, Amount, BalanceAfter, Currency, CreatedAtUtc)
            VALUES
                (@Id, @TransactionId, @WalletId, @Direction, @Amount, @BalanceAfter, 'TRY', @CreatedAtUtc);
            """;

        await connection.ExecuteAsync(ledgerSql, new
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            WalletId = walletId,
            Direction = (int)LedgerDirection.Debit,
            Amount = amount,
            BalanceAfter = wallet.Balance,
            CreatedAtUtc = now
        }, transaction);

        transaction.Commit();

        return OperationResult<BillPaymentResponse>.Success(new BillPaymentResponse
        {
            BillId = billId,
            TransactionId = transactionId,
            Amount = amount,
            WalletBalance = wallet.Balance,
            Status = "Completed"
        });
    }

    /// <inheritdoc />
    public async Task ReverseAsync(
        Guid walletId,
        Guid originalTransactionId,
        Guid billId,
        decimal amount,
        string correlationId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var wallet = await _walletRepository.GetForUpdateAsync(walletId, connection, transaction)
            ?? throw new InvalidOperationException("Wallet not found during bill-payment reversal.");

        wallet.Balance += amount;
        wallet.UpdatedAtUtc = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, connection, transaction);

        var reversalId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        const string txSql = """
            INSERT INTO dbo.WalletTransactions
                (Id, SourceWalletId, DestinationWalletId, Amount, Currency, Type, Status,
                 IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc)
            VALUES
                (@Id, NULL, @WalletId, @Amount, 'TRY', 'BillPaymentReversal', @Status,
                 @IdempotencyKey, @CorrelationId, @CreatedAtUtc, @CompletedAtUtc);
            """;

        await connection.ExecuteAsync(txSql, new
        {
            Id = reversalId,
            WalletId = walletId,
            Amount = amount,
            Status = (int)TransactionStatus.Reversed,
            IdempotencyKey = $"bill-reversal:{originalTransactionId}",
            CorrelationId = correlationId,
            CreatedAtUtc = now,
            CompletedAtUtc = now
        }, transaction);

        const string ledgerSql = """
            INSERT INTO dbo.LedgerEntries
                (Id, TransactionId, WalletId, Direction, Amount, BalanceAfter, Currency, CreatedAtUtc)
            VALUES
                (@Id, @TransactionId, @WalletId, @Direction, @Amount, @BalanceAfter, 'TRY', @CreatedAtUtc);
            """;

        await connection.ExecuteAsync(ledgerSql, new
        {
            Id = Guid.NewGuid(),
            TransactionId = reversalId,
            WalletId = walletId,
            Direction = (int)LedgerDirection.Credit,
            Amount = amount,
            BalanceAfter = wallet.Balance,
            CreatedAtUtc = now
        }, transaction);

        transaction.Commit();
    }
}

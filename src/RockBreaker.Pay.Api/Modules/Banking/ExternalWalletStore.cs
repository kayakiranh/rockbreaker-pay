using System.Data;
using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Domain;
using RockBreaker.Pay.Modules.Wallet.Application;

namespace RockBreaker.Pay.Modules.Banking;

/// <summary>
/// TR: Banka entegrasyonundaki wallet hareketlerini MSSQL transaction ve ledger kayıtlarıyla uygular.
/// EN: Applies wallet movements for bank integration using MSSQL transactions and ledger records.
/// Architecture: Unit of Work + Pessimistic Locking + Ledger + Compensation.
/// </summary>
public sealed class ExternalWalletStore : IExternalWalletStore
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletLimitGuard _limitGuard;

    /// <summary>TR: Store bağımlılıklarını alır. EN: Receives store dependencies. Architecture: Constructor Injection.</summary>
    public ExternalWalletStore(
        IDbConnectionFactory connectionFactory,
        IWalletRepository walletRepository,
        IWalletLimitGuard limitGuard)
    {
        _connectionFactory = connectionFactory;
        _walletRepository = walletRepository;
        _limitGuard = limitGuard;
    }

    /// <inheritdoc />
    public Task<OperationResult<BankTransferResponse>> CreditFromBankAsync(
        Guid walletId,
        decimal amount,
        string idempotencyKey,
        string correlationId) =>
        ApplyAsync(walletId, amount, idempotencyKey, correlationId, creditWallet: true, "BankToWallet");

    /// <inheritdoc />
    public Task<OperationResult<BankTransferResponse>> DebitToBankAsync(
        Guid walletId,
        decimal amount,
        string idempotencyKey,
        string correlationId) =>
        ApplyAsync(walletId, amount, idempotencyKey, correlationId, creditWallet: false, "WalletToBank");

    /// <inheritdoc />
    public async Task ReverseAsync(
        Guid originalTransactionId,
        Guid walletId,
        decimal amount,
        bool creditWallet,
        string correlationId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var wallet = await _walletRepository.GetForUpdateAsync(walletId, connection, transaction)
            ?? throw new InvalidOperationException("Wallet not found during reversal.");

        wallet.Balance += creditWallet ? amount : -amount;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        if (wallet.Balance < 0)
            throw new InvalidOperationException("Reversal would create a negative wallet balance.");

        await _walletRepository.UpdateAsync(wallet, connection, transaction);

        var reversalId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        const string txSql = """
            INSERT INTO dbo.WalletTransactions
                (Id, SourceWalletId, DestinationWalletId, Amount, Currency, Type, Status,
                 IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc)
            VALUES
                (@Id, @SourceWalletId, @DestinationWalletId, @Amount, 'TRY', 'BankTransferReversal', @Status,
                 @IdempotencyKey, @CorrelationId, @CreatedAtUtc, @CompletedAtUtc);
            """;

        await connection.ExecuteAsync(txSql, new
        {
            Id = reversalId,
            SourceWalletId = creditWallet ? (Guid?)null : walletId,
            DestinationWalletId = creditWallet ? walletId : (Guid?)null,
            Amount = amount,
            Status = (int)TransactionStatus.Reversed,
            IdempotencyKey = $"reversal:{originalTransactionId}",
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
            Direction = creditWallet ? (int)LedgerDirection.Credit : (int)LedgerDirection.Debit,
            Amount = amount,
            BalanceAfter = wallet.Balance,
            CreatedAtUtc = now
        }, transaction);

        transaction.Commit();
    }

    private async Task<OperationResult<BankTransferResponse>> ApplyAsync(
        Guid walletId,
        decimal amount,
        string idempotencyKey,
        string correlationId,
        bool creditWallet,
        string type)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        const string duplicateSql = "SELECT COUNT_BIG(1) FROM dbo.WalletTransactions WHERE IdempotencyKey = @IdempotencyKey;";
        if (await connection.ExecuteScalarAsync<long>(duplicateSql, new { IdempotencyKey = idempotencyKey }, transaction) > 0)
        {
            transaction.Rollback();
            return OperationResult<BankTransferResponse>.Fail("DUPLICATE_REQUEST", "Idempotency key already exists.");
        }

        var wallet = await _walletRepository.GetForUpdateAsync(walletId, connection, transaction);
        if (wallet is null)
        {
            transaction.Rollback();
            return OperationResult<BankTransferResponse>.Fail("WALLET_NOT_FOUND", "Wallet was not found.");
        }

        if (wallet.Status != WalletStatus.Active)
        {
            transaction.Rollback();
            return OperationResult<BankTransferResponse>.Fail("WALLET_NOT_ACTIVE", "Wallet is not active.");
        }

        if (!creditWallet)
        {
            var limitResult = await _limitGuard.CheckAsync(wallet, amount, connection, transaction);
            if (!limitResult.IsAllowed)
            {
                transaction.Rollback();
                return OperationResult<BankTransferResponse>.Fail(
                    limitResult.ErrorCode!,
                    limitResult.ErrorMessage!);
            }

            if (wallet.Balance < amount)
            {
                transaction.Rollback();
                return OperationResult<BankTransferResponse>.Fail(
                    "INSUFFICIENT_BALANCE",
                    "Wallet balance is insufficient.");
            }
        }

        wallet.Balance += creditWallet ? amount : -amount;
        wallet.UpdatedAtUtc = DateTime.UtcNow;
        await _walletRepository.UpdateAsync(wallet, connection, transaction);

        var transactionId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        const string txSql = """
            INSERT INTO dbo.WalletTransactions
                (Id, SourceWalletId, DestinationWalletId, Amount, Currency, Type, Status,
                 IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc)
            VALUES
                (@Id, @SourceWalletId, @DestinationWalletId, @Amount, 'TRY', @Type, @Status,
                 @IdempotencyKey, @CorrelationId, @CreatedAtUtc, @CompletedAtUtc);
            """;

        await connection.ExecuteAsync(txSql, new
        {
            Id = transactionId,
            SourceWalletId = creditWallet ? (Guid?)null : walletId,
            DestinationWalletId = creditWallet ? walletId : (Guid?)null,
            Amount = amount,
            Type = type,
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
            Direction = creditWallet ? (int)LedgerDirection.Credit : (int)LedgerDirection.Debit,
            Amount = amount,
            BalanceAfter = wallet.Balance,
            CreatedAtUtc = now
        }, transaction);

        transaction.Commit();

        return OperationResult<BankTransferResponse>.Success(new BankTransferResponse
        {
            TransactionId = transactionId,
            Type = type,
            Amount = amount,
            WalletBalance = wallet.Balance
        });
    }
}

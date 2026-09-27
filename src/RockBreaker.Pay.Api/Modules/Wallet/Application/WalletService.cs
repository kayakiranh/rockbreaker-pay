using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Contracts;
using RockBreaker.Pay.Modules.Wallet.Domain;
using WalletEntity = RockBreaker.Pay.Modules.Wallet.Domain.Wallet;

namespace RockBreaker.Pay.Modules.Wallet.Application;

/// <summary>
/// TR: Wallet oluşturma, sorgulama, hareket ve limit use-case'lerini orkestre eder.
/// EN: Orchestrates wallet creation, query, movement and limit use cases.
/// Architecture: Application Service + Repository.
/// </summary>
public sealed class WalletService : IWalletService
{
    private const decimal SystemSingleLimit = 100_000m;
    private const decimal SystemDailyLimit = 250_000m;
    private const decimal SystemMonthlyLimit = 1_000_000m;

    private readonly IWalletRepository _repository;

    /// <summary>TR: Wallet service bağımlılıklarını alır. EN: Receives wallet-service dependencies. Architecture: Constructor Injection.</summary>
    /// <param name="repository">TR: Wallet repository. EN: Wallet repository.</param>
    public WalletService(IWalletRepository repository) => _repository = repository;

    /// <inheritdoc />
    public async Task<OperationResult<WalletSummaryResponse>> CreateAsync(Guid userId)
    {
        if (await _repository.GetByUserIdAsync(userId) is not null)
            return OperationResult<WalletSummaryResponse>.Fail("WALLET_EXISTS", "User already has a wallet.");

        var now = DateTime.UtcNow;
        var wallet = new WalletEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 0,
            Currency = "TRY",
            Status = WalletStatus.Active,
            SingleTransactionLimit = 25_000m,
            DailyLimit = 50_000m,
            MonthlyLimit = 250_000m,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _repository.InsertAsync(wallet);
        return OperationResult<WalletSummaryResponse>.Success(Map(wallet));
    }

    /// <inheritdoc />
    public async Task<OperationResult<WalletSummaryResponse>> GetAsync(Guid userId)
    {
        var wallet = await _repository.GetByUserIdAsync(userId);
        return wallet is null
            ? OperationResult<WalletSummaryResponse>.Fail("WALLET_NOT_FOUND", "Wallet was not found.")
            : OperationResult<WalletSummaryResponse>.Success(Map(wallet));
    }

    /// <inheritdoc />
    public async Task<OperationResult<IReadOnlyCollection<WalletMovementResponse>>> GetMovementsAsync(Guid userId)
    {
        var wallet = await _repository.GetByUserIdAsync(userId);
        if (wallet is null)
            return OperationResult<IReadOnlyCollection<WalletMovementResponse>>.Fail("WALLET_NOT_FOUND", "Wallet was not found.");

        var transactions = await _repository.GetTransactionsAsync(wallet.Id);
        var movements = transactions.Select(x => new WalletMovementResponse
        {
            TransactionId = x.Id,
            Type = x.Type,
            Amount = x.Amount,
            Currency = x.Currency,
            Status = x.Status.ToString(),
            Direction = x.SourceWalletId == wallet.Id ? "Debit" : "Credit",
            CreatedAtUtc = x.CreatedAtUtc
        }).ToArray();

        return OperationResult<IReadOnlyCollection<WalletMovementResponse>>.Success(movements);
    }

    /// <inheritdoc />
    public async Task<OperationResult<WalletSummaryResponse>> UpdateLimitsAsync(
        Guid userId,
        UpdateWalletLimitsRequest request)
    {
        if (request.SingleTransactionLimit <= 0 ||
            request.DailyLimit <= 0 ||
            request.MonthlyLimit <= 0)
            return OperationResult<WalletSummaryResponse>.Fail("INVALID_LIMIT", "Limits must be greater than zero.");

        if (request.SingleTransactionLimit > SystemSingleLimit ||
            request.DailyLimit > SystemDailyLimit ||
            request.MonthlyLimit > SystemMonthlyLimit)
            return OperationResult<WalletSummaryResponse>.Fail("SYSTEM_LIMIT_EXCEEDED", "Requested limit exceeds system maximum.");

        if (request.SingleTransactionLimit > request.DailyLimit || request.DailyLimit > request.MonthlyLimit)
            return OperationResult<WalletSummaryResponse>.Fail("INVALID_LIMIT_ORDER", "Single <= daily <= monthly is required.");

        var wallet = await _repository.GetByUserIdAsync(userId);
        if (wallet is null)
            return OperationResult<WalletSummaryResponse>.Fail("WALLET_NOT_FOUND", "Wallet was not found.");

        wallet.SingleTransactionLimit = request.SingleTransactionLimit;
        wallet.DailyLimit = request.DailyLimit;
        wallet.MonthlyLimit = request.MonthlyLimit;
        wallet.UpdatedAtUtc = DateTime.UtcNow;

        await _repository.UpdateLimitsAsync(wallet);
        return OperationResult<WalletSummaryResponse>.Success(Map(wallet));
    }

    private static WalletSummaryResponse Map(WalletEntity wallet) => new()
    {
        Id = wallet.Id,
        Balance = wallet.Balance,
        Currency = wallet.Currency,
        Status = wallet.Status.ToString(),
        SingleTransactionLimit = wallet.SingleTransactionLimit,
        DailyLimit = wallet.DailyLimit,
        MonthlyLimit = wallet.MonthlyLimit
    };
}

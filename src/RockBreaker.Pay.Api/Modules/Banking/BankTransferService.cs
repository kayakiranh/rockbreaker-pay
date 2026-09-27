using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Compliance;
using RockBreaker.Pay.Modules.Cutoff;
using RockBreaker.Pay.Modules.Fraud;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Domain;

namespace RockBreaker.Pay.Modules.Banking;

/// <summary>
/// TR: Banka-wallet transferlerinde cutoff, fraud, local ledger ve remote banka çağrılarını orkestre eder.
/// EN: Orchestrates cutoff, fraud, local ledger and remote bank calls for bank-wallet transfers.
/// Architecture: Saga Orchestration + Compensation.
/// </summary>
public sealed class BankTransferService : IBankTransferService
{
    private readonly IWalletRepository _walletRepository;
    private readonly ICutoffClient _cutoffClient;
    private readonly IFraudEvaluator _fraudEvaluator;
    private readonly IBankingClient _bankingClient;
    private readonly IExternalWalletStore _walletStore;
    private readonly IKycGuard _kycGuard;

    /// <summary>TR: Servis bağımlılıklarını alır. EN: Receives service dependencies. Architecture: Constructor Injection.</summary>
    public BankTransferService(
        IWalletRepository walletRepository,
        ICutoffClient cutoffClient,
        IFraudEvaluator fraudEvaluator,
        IBankingClient bankingClient,
        IExternalWalletStore walletStore,
        IKycGuard kycGuard)
    {
        _walletRepository = walletRepository;
        _cutoffClient = cutoffClient;
        _fraudEvaluator = fraudEvaluator;
        _bankingClient = bankingClient;
        _walletStore = walletStore;
        _kycGuard = kycGuard;
    }

    /// <inheritdoc />
    public async Task<OperationResult<BankTransferResponse>> BankToWalletAsync(
        Guid userId,
        BankTransferRequest request,
        string idempotencyKey,
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return OperationResult<BankTransferResponse>.Fail(
                "IDEMPOTENCY_KEY_REQUIRED",
                "Idempotency-Key header is required.");
        }

        if (!await _kycGuard.IsUserVerifiedAsync(userId))
            return OperationResult<BankTransferResponse>.Fail("KYC_REQUIRED", "Verified KYC is required for bank transfers.");

        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet is null)
            return OperationResult<BankTransferResponse>.Fail("WALLET_NOT_FOUND", "Wallet was not found.");

        var guard = await ValidateAsync(wallet.Id, request.Amount, "BankToWallet", blockOnFraud: true);
        if (guard is not null)
            return guard;

        var bankSucceeded = await _bankingClient.TransferToWalletAsync(
            request.BankAccountId,
            wallet.Id,
            request.Amount);

        if (!bankSucceeded)
            return OperationResult<BankTransferResponse>.Fail("BANK_TRANSFER_FAILED", "Bank rejected the transfer.");

        var local = await _walletStore.CreditFromBankAsync(
            wallet.Id,
            request.Amount,
            idempotencyKey,
            correlationId);

        if (!local.IsSuccess)
        {
            await _bankingClient.ReverseAsync(request.BankAccountId, wallet.Id, request.Amount, "BankToWallet");
        }

        return local;
    }

    /// <inheritdoc />
    public async Task<OperationResult<BankTransferResponse>> WalletToBankAsync(
        Guid userId,
        BankTransferRequest request,
        string idempotencyKey,
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return OperationResult<BankTransferResponse>.Fail(
                "IDEMPOTENCY_KEY_REQUIRED",
                "Idempotency-Key header is required.");
        }

        if (!await _kycGuard.IsUserVerifiedAsync(userId))
            return OperationResult<BankTransferResponse>.Fail("KYC_REQUIRED", "Verified KYC is required for bank transfers.");

        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet is null)
            return OperationResult<BankTransferResponse>.Fail("WALLET_NOT_FOUND", "Wallet was not found.");

        var guard = await ValidateAsync(wallet.Id, request.Amount, "WalletToBank", blockOnFraud: true);
        if (guard is not null)
            return guard;

        var local = await _walletStore.DebitToBankAsync(
            wallet.Id,
            request.Amount,
            idempotencyKey,
            correlationId);

        if (!local.IsSuccess)
            return local;

        var bankSucceeded = await _bankingClient.TransferFromWalletAsync(
            request.BankAccountId,
            wallet.Id,
            request.Amount);

        if (bankSucceeded)
            return local;

        await _walletStore.ReverseAsync(
            local.Data!.TransactionId,
            wallet.Id,
            request.Amount,
            creditWallet: true,
            correlationId);

        return OperationResult<BankTransferResponse>.Fail(
            "BANK_TRANSFER_FAILED",
            "Bank transfer failed and wallet debit was compensated.");
    }

    private async Task<OperationResult<BankTransferResponse>?> ValidateAsync(
        Guid walletId,
        decimal amount,
        string operationType,
        bool blockOnFraud)
    {
        if (amount <= 0)
            return OperationResult<BankTransferResponse>.Fail("INVALID_AMOUNT", "Amount must be greater than zero.");

        if (!await _cutoffClient.IsOperationAllowedAsync(operationType))
            return OperationResult<BankTransferResponse>.Fail("CUTOFF_CLOSED", "Bank transfer is not available at this time.");

        var fraud = await _fraudEvaluator.EvaluateAsync(walletId, amount);
        if (fraud.Action == FraudAction.BlockWallet && blockOnFraud)
        {
            await _walletRepository.UpdateStatusAsync(walletId, WalletStatus.Blocked);
            return OperationResult<BankTransferResponse>.Fail("WALLET_BLOCKED_BY_FRAUD", "Wallet was blocked by fraud rules.");
        }

        if (fraud.Action == FraudAction.Reject)
            return OperationResult<BankTransferResponse>.Fail("FRAUD_REJECTED", "Transaction was rejected by fraud rules.");

        return null;
    }
}

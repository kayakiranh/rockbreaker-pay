using FluentValidation;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Compliance;
using RockBreaker.Pay.Modules.Fraud;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Contracts;
using RockBreaker.Pay.Modules.Wallet.Domain;

namespace RockBreaker.Pay.Modules.Wallet.Application;

/// <summary>
/// TR: Wallet transfer use-case'inin validasyon, fraud ve persistence sırasını yönetir.
/// EN: Coordinates validation, fraud evaluation and persistence for the wallet transfer use case.
/// Architecture: Application Service.
/// </summary>
public sealed class WalletTransferService : IWalletTransferService
{
    private readonly IValidator<TransferRequest> _validator;
    private readonly IFraudEvaluator _fraudEvaluator;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTransferStore _transferStore;
    private readonly IKycGuard _kycGuard;

    /// <summary>
    /// TR: Transfer servisinin bağımlılıklarını alır.
    /// EN: Receives transfer service dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="validator">TR: Request validator. EN: Request validator.</param>
    /// <param name="fraudEvaluator">TR: Fraud evaluator. EN: Fraud evaluator.</param>
    /// <param name="walletRepository">TR: Wallet repository. EN: Wallet repository.</param>
    /// <param name="transferStore">TR: Atomik transfer store. EN: Atomic transfer store.</param>
    public WalletTransferService(
        IValidator<TransferRequest> validator,
        IFraudEvaluator fraudEvaluator,
        IWalletRepository walletRepository,
        IWalletTransferStore transferStore,
        IKycGuard kycGuard)
    {
        _validator = validator;
        _fraudEvaluator = fraudEvaluator;
        _walletRepository = walletRepository;
        _transferStore = transferStore;
        _kycGuard = kycGuard;
    }

    /// <inheritdoc />
    public async Task<OperationResult<TransferResponse>> TransferForUserAsync(
        Guid userId,
        TransferRequest request,
        string idempotencyKey,
        string correlationId)
    {
        var sourceWallet = await _walletRepository.GetByIdAsync(request.SourceWalletId);
        if (sourceWallet is null)
        {
            return OperationResult<TransferResponse>.Fail(
                "WALLET_NOT_FOUND",
                "Source wallet was not found.");
        }

        if (sourceWallet.UserId != userId)
        {
            return OperationResult<TransferResponse>.Fail(
                "FORBIDDEN_SOURCE_WALLET",
                "Authenticated user does not own the source wallet.");
        }

        return await TransferAsync(request, idempotencyKey, correlationId);
    }

    /// <inheritdoc />
    public async Task<OperationResult<TransferResponse>> TransferAsync(
        TransferRequest request,
        string idempotencyKey,
        string correlationId)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return OperationResult<TransferResponse>.Fail(
                "VALIDATION_ERROR",
                string.Join(" | ", validation.Errors.Select(x => x.ErrorMessage)));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return OperationResult<TransferResponse>.Fail(
                "IDEMPOTENCY_KEY_REQUIRED",
                "Idempotency-Key header is required.");
        }

        if (!await _kycGuard.IsWalletOwnerVerifiedAsync(request.SourceWalletId))
        {
            return OperationResult<TransferResponse>.Fail(
                "KYC_REQUIRED",
                "Source wallet owner must have Verified KYC status.");
        }

        if (!await _kycGuard.IsWalletOwnerVerifiedAsync(request.DestinationWalletId))
        {
            return OperationResult<TransferResponse>.Fail(
                "DESTINATION_KYC_REQUIRED",
                "Destination wallet owner must have Verified KYC status.");
        }

        var fraud = await _fraudEvaluator.EvaluateAsync(request.SourceWalletId, request.Amount);

        if (fraud.Action == FraudAction.BlockWallet)
        {
            await _walletRepository.UpdateStatusAsync(request.SourceWalletId, WalletStatus.Blocked);
            return OperationResult<TransferResponse>.Fail(
                "WALLET_BLOCKED_BY_FRAUD",
                $"Wallet was blocked. Triggered rules: {string.Join(",", fraud.TriggeredRules)}.");
        }

        if (fraud.Action == FraudAction.Reject)
        {
            return OperationResult<TransferResponse>.Fail(
                "FRAUD_REJECTED",
                $"Transaction was rejected. Triggered rules: {string.Join(",", fraud.TriggeredRules)}.");
        }

        return await _transferStore.ExecuteAsync(request, idempotencyKey, correlationId);
    }
}

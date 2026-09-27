using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Compliance;
using RockBreaker.Pay.Modules.Fraud;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Domain;

namespace RockBreaker.Pay.Modules.Government;

/// <summary>
/// TR: Fatura ödeme akışında SOAP sorgusu, fraud, wallet ledger ve telafi adımlarını orkestre eder.
/// EN: Orchestrates SOAP lookup, fraud, wallet ledger and compensation steps for bill payment.
/// Architecture: Saga Orchestration + Compensation.
/// </summary>
public sealed class BillPaymentService : IBillPaymentService
{
    private readonly IGovernmentSoapClient _governmentClient;
    private readonly IWalletRepository _walletRepository;
    private readonly IFraudEvaluator _fraudEvaluator;
    private readonly IBillPaymentStore _paymentStore;
    private readonly IKycGuard _kycGuard;

    /// <summary>
    /// TR: Servis bağımlılıklarını alır.
    /// EN: Receives service dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public BillPaymentService(
        IGovernmentSoapClient governmentClient,
        IWalletRepository walletRepository,
        IFraudEvaluator fraudEvaluator,
        IBillPaymentStore paymentStore,
        IKycGuard kycGuard)
    {
        _governmentClient = governmentClient;
        _walletRepository = walletRepository;
        _fraudEvaluator = fraudEvaluator;
        _paymentStore = paymentStore;
        _kycGuard = kycGuard;
    }

    /// <inheritdoc />
    public async Task<OperationResult<IReadOnlyCollection<BillResponse>>> GetBillsAsync(string citizenNumber) =>
        OperationResult<IReadOnlyCollection<BillResponse>>.Success(
            await _governmentClient.GetBillsAsync(citizenNumber));

    /// <inheritdoc />
    public async Task<OperationResult<BillPaymentResponse>> PayAsync(
        Guid userId,
        string citizenNumber,
        Guid billId,
        string idempotencyKey,
        string correlationId)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return OperationResult<BillPaymentResponse>.Fail(
                "IDEMPOTENCY_KEY_REQUIRED",
                "Idempotency-Key header is required.");
        }

        if (!await _kycGuard.IsUserVerifiedAsync(userId))
        {
            return OperationResult<BillPaymentResponse>.Fail(
                "KYC_REQUIRED",
                "Verified KYC is required for bill payments.");
        }

        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet is null)
        {
            return OperationResult<BillPaymentResponse>.Fail(
                "WALLET_NOT_FOUND",
                "Wallet was not found.");
        }

        var bills = await _governmentClient.GetBillsAsync(citizenNumber);
        var bill = bills.FirstOrDefault(x => x.Id == billId && !x.IsPaid);
        if (bill is null)
        {
            return OperationResult<BillPaymentResponse>.Fail(
                "BILL_NOT_FOUND",
                "Open bill was not found.");
        }

        var fraud = await _fraudEvaluator.EvaluateAsync(wallet.Id, bill.Amount);
        if (fraud.Action == FraudAction.BlockWallet)
        {
            await _walletRepository.UpdateStatusAsync(wallet.Id, WalletStatus.Blocked);
            return OperationResult<BillPaymentResponse>.Fail(
                "WALLET_BLOCKED_BY_FRAUD",
                "Wallet was blocked by fraud rules.");
        }

        if (fraud.Action == FraudAction.Reject)
        {
            return OperationResult<BillPaymentResponse>.Fail(
                "FRAUD_REJECTED",
                "Bill payment was rejected by fraud rules.");
        }

        var debit = await _paymentStore.DebitAsync(
            wallet.Id,
            bill.Id,
            bill.Amount,
            idempotencyKey,
            correlationId);

        if (!debit.IsSuccess)
        {
            return debit;
        }

        if (await _governmentClient.PayBillAsync(bill.Id))
        {
            await _paymentStore.MarkNotificationReadyAsync(debit.Data!.TransactionId);
            return debit;
        }

        await _paymentStore.ReverseAsync(
            wallet.Id,
            debit.Data!.TransactionId,
            bill.Id,
            bill.Amount,
            correlationId);

        return OperationResult<BillPaymentResponse>.Fail(
            "GOVERNMENT_PAYMENT_FAILED",
            "Government payment failed and wallet debit was compensated.");
    }
}

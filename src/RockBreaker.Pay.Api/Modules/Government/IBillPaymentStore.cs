using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.Government;

/// <summary>
/// TR: Fatura ödemesinin wallet debit ve reversal kayıtlarını atomik olarak yönetir.
/// EN: Atomically manages wallet debit and reversal records for bill payments.
/// Architecture: Specialized Unit of Work + Ledger + Compensation.
/// </summary>
public interface IBillPaymentStore
{
    /// <summary>
    /// TR: Fatura tutarını wallet'tan düşüp transaction ve ledger kaydı oluşturur.
    /// EN: Debits the bill amount from the wallet and creates transaction and ledger records.
    /// Architecture: Atomic Financial Command.
    /// </summary>
    Task<OperationResult<BillPaymentResponse>> DebitAsync(
        Guid walletId,
        Guid billId,
        decimal amount,
        string idempotencyKey,
        string correlationId);

    /// <summary>
    /// TR: SOAP ödeme başarısız olduğunda wallet debit işlemini ters kayıtla telafi eder.
    /// EN: Compensates the wallet debit with a reversing entry when SOAP payment fails.
    /// Architecture: Compensating Transaction.
    /// </summary>
    Task ReverseAsync(
        Guid walletId,
        Guid originalTransactionId,
        Guid billId,
        decimal amount,
        string correlationId);
}

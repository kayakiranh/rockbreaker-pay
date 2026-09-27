using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.Banking;

/// <summary>
/// TR: Banka entegrasyonu kaynaklı wallet debit/credit ve reversal kayıtlarını atomik olarak uygular.
/// EN: Atomically applies wallet debit/credit and reversal records originating from bank integration.
/// Architecture: Specialized Unit of Work + Ledger.
/// </summary>
public interface IExternalWalletStore
{
    /// <summary>TR: Wallet'a banka kaynaklı kredi yazar. EN: Credits wallet from bank. Architecture: Atomic Financial Command.</summary>
    Task<OperationResult<BankTransferResponse>> CreditFromBankAsync(Guid walletId, decimal amount, string idempotencyKey, string correlationId);
    /// <summary>TR: Wallet'tan banka yönlü debit yazar. EN: Debits wallet for bank transfer. Architecture: Atomic Financial Command.</summary>
    Task<OperationResult<BankTransferResponse>> DebitToBankAsync(Guid walletId, decimal amount, string idempotencyKey, string correlationId);
    /// <summary>TR: Başarısız dış servis çağrısı için ters ledger kaydı oluşturur. EN: Creates a reversing ledger entry for failed external calls. Architecture: Compensating Transaction.</summary>
    Task ReverseAsync(Guid originalTransactionId, Guid walletId, decimal amount, bool creditWallet, string correlationId);
}

namespace RockBreaker.Pay.Modules.Banking;

/// <summary>
/// TR: FakeBanking dış servisine erişimi soyutlar.
/// EN: Abstracts access to the external FakeBanking service.
/// Architecture: Anti-Corruption Layer + Dependency Inversion.
/// </summary>
public interface IBankingClient
{
    /// <summary>TR: Bankadan wallet'a para çıkışı yaptırır. EN: Debits bank account for a bank-to-wallet transfer. Architecture: External Command.</summary>
    Task<bool> TransferToWalletAsync(Guid bankAccountId, Guid walletId, decimal amount);
    /// <summary>TR: Wallet'tan bankaya para girişi yaptırır. EN: Credits bank account for a wallet-to-bank transfer. Architecture: External Command.</summary>
    Task<bool> TransferFromWalletAsync(Guid bankAccountId, Guid walletId, decimal amount);
    /// <summary>TR: Önceki fake banka hareketini telafi eder. EN: Compensates a previous fake bank movement. Architecture: Saga Compensation.</summary>
    Task<bool> ReverseAsync(Guid bankAccountId, Guid walletId, decimal amount, string originalType);
}

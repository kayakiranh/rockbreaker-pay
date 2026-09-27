using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.Banking;

/// <summary>
/// TR: Banka-wallet transfer use-case'lerini tanımlar.
/// EN: Defines bank-wallet transfer use cases.
/// Architecture: Application Service Interface.
/// </summary>
public interface IBankTransferService
{
    /// <summary>TR: Bankadan wallet'a transfer yapar. EN: Transfers from bank to wallet. Architecture: Saga Command.</summary>
    Task<OperationResult<BankTransferResponse>> BankToWalletAsync(Guid userId, BankTransferRequest request, string idempotencyKey, string correlationId);
    /// <summary>TR: Wallet'tan bankaya transfer yapar. EN: Transfers from wallet to bank. Architecture: Saga Command.</summary>
    Task<OperationResult<BankTransferResponse>> WalletToBankAsync(Guid userId, BankTransferRequest request, string idempotencyKey, string correlationId);
}

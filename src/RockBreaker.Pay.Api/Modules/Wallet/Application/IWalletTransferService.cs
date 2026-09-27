using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.Wallet.Application;

/// <summary>
/// TR: Wallet transfer use-case'ini orkestre eden application service sözleşmesidir.
/// EN: Application service contract orchestrating the wallet transfer use case.
/// Architecture: Application Service + Dependency Inversion.
/// </summary>
public interface IWalletTransferService
{
    /// <summary>
    /// TR: Validasyon sonrası fraud kontrolünü çalıştırır ve transferi atomik store'a yönlendirir.
    /// EN: Runs fraud evaluation after validation and delegates the transfer to the atomic store.
    /// Architecture: Application Service orchestration.
    /// </summary>
    /// <param name="request">TR: Transfer isteği. EN: Transfer request.</param>
    /// <param name="idempotencyKey">TR: Idempotency anahtarı. EN: Idempotency key.</param>
    /// <param name="correlationId">TR: Takip kimliği. EN: Correlation identifier.</param>
    /// <returns>TR: İşlem sonucu. EN: Operation result.</returns>
    Task<OperationResult<TransferResponse>> TransferAsync(
        TransferRequest request,
        string idempotencyKey,
        string correlationId);
}

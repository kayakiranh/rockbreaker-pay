using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.Wallet.Abstractions;

/// <summary>
/// TR: Wallet transferinin tüm SQL değişikliklerini tek atomik transaction içinde uygular.
/// EN: Applies all SQL mutations of a wallet transfer inside one atomic transaction.
/// Architecture: Unit of Work boundary specialized for financial transfer consistency.
/// </summary>
public interface IWalletTransferStore
{
    /// <summary>
    /// TR: Bakiye, transaction, double-entry ledger ve outbox kayıtlarını tek transaction içinde oluşturur.
    /// EN: Creates balance changes, transaction header, double-entry ledger entries and outbox record in one transaction.
    /// Architecture: Unit of Work + Double-Entry Ledger + Transactional Outbox.
    /// </summary>
    /// <param name="request">TR: Transfer isteği. EN: Transfer request.</param>
    /// <param name="idempotencyKey">TR: Tekrarlı isteği engelleyen anahtar. EN: Key preventing duplicate execution.</param>
    /// <param name="correlationId">TR: Takip kimliği. EN: Correlation identifier.</param>
    /// <returns>TR: Transfer sonucu. EN: Transfer result.</returns>
    Task<OperationResult<TransferResponse>> ExecuteAsync(
        TransferRequest request,
        string idempotencyKey,
        string correlationId);
}

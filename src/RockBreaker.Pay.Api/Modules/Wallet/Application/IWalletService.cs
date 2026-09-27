using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.Wallet.Application;

/// <summary>
/// TR: Wallet oluşturma, görüntüleme, hareket ve limit use-case'lerini tanımlar.
/// EN: Defines wallet creation, retrieval, movement and limit use cases.
/// Architecture: Application Service Interface.
/// </summary>
public interface IWalletService
{
    /// <summary>TR: Kullanıcı için wallet oluşturur. EN: Creates a wallet for a user. Architecture: Application Command.</summary>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    /// <returns>TR: Wallet sonucu. EN: Wallet result.</returns>
    Task<OperationResult<WalletSummaryResponse>> CreateAsync(Guid userId);

    /// <summary>TR: Kullanıcının wallet özetini getirir. EN: Gets the user's wallet summary. Architecture: Application Query.</summary>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    /// <returns>TR: Wallet sonucu. EN: Wallet result.</returns>
    Task<OperationResult<WalletSummaryResponse>> GetAsync(Guid userId);

    /// <summary>TR: Kullanıcının wallet hareketlerini listeler. EN: Lists the user's wallet movements. Architecture: Application Query.</summary>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    /// <returns>TR: Hareketler. EN: Movements.</returns>
    Task<OperationResult<IReadOnlyCollection<WalletMovementResponse>>> GetMovementsAsync(Guid userId);

    /// <summary>TR: Kullanıcı limitlerini günceller. EN: Updates user limits. Architecture: Application Command.</summary>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    /// <param name="request">TR: Limit isteği. EN: Limit request.</param>
    /// <returns>TR: Güncel wallet. EN: Updated wallet.</returns>
    Task<OperationResult<WalletSummaryResponse>> UpdateLimitsAsync(Guid userId, UpdateWalletLimitsRequest request);
}

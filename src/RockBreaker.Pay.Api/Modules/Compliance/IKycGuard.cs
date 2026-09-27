namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: Para hareketlerinden önce KYC uygunluğunu merkezi olarak kontrol eder.
/// EN: Centrally checks KYC eligibility before money movements.
/// Architecture: Compliance Policy + Dependency Inversion.
/// </summary>
public interface IKycGuard
{
    /// <summary>
    /// TR: Kullanıcının Verified KYC durumunda olup olmadığını döndürür.
    /// EN: Returns whether the user has Verified KYC status.
    /// Architecture: Compliance Policy Query.
    /// </summary>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    Task<bool> IsUserVerifiedAsync(Guid userId);

    /// <summary>
    /// TR: Wallet sahibinin Verified KYC durumunda olup olmadığını döndürür.
    /// EN: Returns whether the wallet owner has Verified KYC status.
    /// Architecture: Compliance Policy Query.
    /// </summary>
    /// <param name="walletId">TR: Wallet kimliği. EN: Wallet identifier.</param>
    Task<bool> IsWalletOwnerVerifiedAsync(Guid walletId);
}

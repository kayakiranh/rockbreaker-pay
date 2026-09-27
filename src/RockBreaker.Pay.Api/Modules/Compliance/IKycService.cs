using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: KYC başlatma, görüntüleme ve admin inceleme use-case'lerini tanımlar.
/// EN: Defines KYC submission, viewing and admin-review use cases.
/// Architecture: Application Service Interface.
/// </summary>
public interface IKycService
{
    /// <summary>TR: Kullanıcının kendi KYC durumunu getirir. EN: Gets the user's own KYC status. Architecture: Application Query.</summary>
    Task<OperationResult<KycStatusResponse>> GetAsync(Guid userId);

    /// <summary>TR: Kullanıcı KYC sürecini Pending durumuna taşır. EN: Moves the user's KYC process to Pending. Architecture: Application Command.</summary>
    Task<OperationResult<KycStatusResponse>> SubmitAsync(Guid userId);

    /// <summary>TR: Admin KYC sonucunu Verified/Rejected olarak günceller. EN: Admin updates KYC result to Verified/Rejected. Architecture: Application Command.</summary>
    Task<OperationResult<KycStatusResponse>> ReviewAsync(
        Guid actorUserId,
        Guid targetUserId,
        ReviewKycRequest request);
}

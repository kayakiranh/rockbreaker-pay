using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.Campaigns;

/// <summary>
/// TR: Kampanya listeleme, katılım ve kampanyalı transfer use-case'lerini tanımlar.
/// EN: Defines campaign listing, participation and campaign-transfer use cases.
/// Architecture: Application Service Interface + Dependency Inversion.
/// </summary>
public interface ICampaignService
{
    /// <summary>TR: Aktif kampanyaları listeler. EN: Lists active campaigns. Architecture: Application Query.</summary>
    Task<IReadOnlyCollection<CampaignResponse>> GetAsync();

    /// <summary>TR: KYC doğrulanmış kullanıcıyı kampanyaya dahil eder. EN: Enrolls a KYC-verified user in a campaign. Architecture: Application Command.</summary>
    Task<OperationResult<CampaignParticipationResponse>> JoinAsync(Guid userId, Guid campaignId);

    /// <summary>TR: Kampanya katılımını doğrulayıp core transfer use-case'ini çalıştırır. EN: Verifies campaign participation and executes the core transfer use case. Architecture: Application Orchestration.</summary>
    Task<OperationResult<CampaignTransferResponse>> TransferAsync(
        Guid userId,
        Guid campaignId,
        CampaignTransferRequest request,
        string idempotencyKey,
        string correlationId);
}

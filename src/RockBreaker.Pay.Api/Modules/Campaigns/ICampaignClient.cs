namespace RockBreaker.Pay.Modules.Campaigns;

/// <summary>
/// TR: FakeCampaign dış servisine erişimi soyutlar.
/// EN: Abstracts access to the external FakeCampaign service.
/// Architecture: Anti-Corruption Layer + Dependency Inversion.
/// </summary>
public interface ICampaignClient
{
    /// <summary>TR: Aktif kampanyaları getirir. EN: Retrieves active campaigns. Architecture: External Query Port.</summary>
    Task<IReadOnlyCollection<CampaignResponse>> GetCampaignsAsync();

    /// <summary>TR: Kullanıcıyı kampanyaya dahil eder. EN: Enrolls the user in a campaign. Architecture: External Command Port.</summary>
    Task<bool> JoinAsync(Guid campaignId, Guid userId);

    /// <summary>TR: Kullanıcının kampanyaya katılımını sorgular. EN: Checks whether the user participates in a campaign. Architecture: External Query Port.</summary>
    Task<bool> IsJoinedAsync(Guid campaignId, Guid userId);
}

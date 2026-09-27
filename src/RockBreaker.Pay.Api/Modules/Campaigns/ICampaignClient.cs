namespace RockBreaker.Pay.Modules.Campaigns;

/// <summary>
/// TR: FakeCampaign dış servisine erişimi soyutlar.
/// EN: Abstracts access to the external FakeCampaign service.
/// Architecture: Anti-Corruption Layer + Dependency Inversion.
/// </summary>
public interface ICampaignClient
{
    /// <summary>
    /// TR: Dış kampanya servisindeki aktif kampanyaları getirir.
/// EN: Retrieves active campaigns from the external campaign service.
/// Architecture: External Query Port.
    /// </summary>
    /// <returns>TR: Kampanya listesi. EN: Campaign list.</returns>
    Task<IReadOnlyCollection<CampaignResponse>> GetCampaignsAsync();
}

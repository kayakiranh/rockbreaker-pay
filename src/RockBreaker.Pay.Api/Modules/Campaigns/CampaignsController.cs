using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Campaigns;

/// <summary>
/// TR: Wallet kullanıcısının görüntüleyebileceği kampanya endpoint'lerini sunar.
/// EN: Exposes campaign endpoints visible to wallet users.
/// Architecture: Thin REST Controller.
/// </summary>
[ApiController]
[Authorize]
[Route("api/campaigns")]
public sealed class CampaignsController : ControllerBase
{
    private readonly ICampaignClient _campaignClient;

    /// <summary>
    /// TR: Controller bağımlılıklarını alır.
/// EN: Receives controller dependencies.
/// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="campaignClient">TR: Kampanya dış servis adapter'ı. EN: Campaign external-service adapter.</param>
    public CampaignsController(ICampaignClient campaignClient) => _campaignClient = campaignClient;

    /// <summary>
    /// TR: Aktif kampanyaları listeler.
/// EN: Lists active campaigns.
/// Architecture: REST Query.
    /// </summary>
    /// <returns>TR: Kampanya listesi. EN: Campaign list.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CampaignResponse>>> GetAsync() =>
        Ok(await _campaignClient.GetCampaignsAsync());
}

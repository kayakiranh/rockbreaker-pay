using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.FakeCampaign.Controllers;

/// <summary>
/// TR: Dummy firmaların kampanyalarını simüle eder.
/// EN: Simulates campaigns published by dummy companies.
/// Architecture: Fake External Service + REST Query.
/// </summary>
[ApiController]
[Route("api/campaigns")]
public sealed class CampaignsController : ControllerBase
{
    private static readonly List<Campaign> Campaigns =
    [
        new() { Id = Guid.NewGuid(), Company = "Coffee Lab", Title = "500 TL harcamaya 50 TL cashback", MinimumAmount = 500, CashbackAmount = 50 },
        new() { Id = Guid.NewGuid(), Company = "MarketX", Title = "1000 TL harcamaya 100 TL cashback", MinimumAmount = 1000, CashbackAmount = 100 }
    ];

    /// <summary>
    /// TR: Aktif dummy kampanyaları listeler.
    /// EN: Lists active dummy campaigns.
    /// Architecture: REST Query.
    /// </summary>
    /// <returns>TR: Kampanyalar. EN: Campaigns.</returns>
    [HttpGet]
    public ActionResult<IReadOnlyCollection<Campaign>> GetCampaigns() => Ok(Campaigns);
}

/// <summary>
/// TR: Fake kampanya bilgisini temsil eder.
/// EN: Represents fake campaign information.
/// Architecture: Response DTO.
/// </summary>
public sealed class Campaign
{
    /// <summary>TR: Kampanya kimliği. EN: Campaign identifier. Architecture: DTO Property.</summary>
    public Guid Id { get; init; }
    /// <summary>TR: Firma adı. EN: Company name. Architecture: DTO Property.</summary>
    public string Company { get; init; } = string.Empty;
    /// <summary>TR: Kampanya başlığı. EN: Campaign title. Architecture: DTO Property.</summary>
    public string Title { get; init; } = string.Empty;
    /// <summary>TR: Kampanya minimum harcama tutarı. EN: Campaign minimum spend. Architecture: DTO Property.</summary>
    public decimal MinimumAmount { get; init; }
    /// <summary>TR: Cashback tutarı. EN: Cashback amount. Architecture: DTO Property.</summary>
    public decimal CashbackAmount { get; init; }
}

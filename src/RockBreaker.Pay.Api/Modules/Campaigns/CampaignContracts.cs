namespace RockBreaker.Pay.Modules.Campaigns;

/// <summary>
/// TR: Wallet kullanıcısına gösterilecek kampanya bilgisini temsil eder.
/// EN: Represents campaign information displayed to a wallet user.
/// Architecture: Response DTO; external service model is not exposed directly.
/// </summary>
public sealed class CampaignResponse
{
    /// <summary>TR: Kampanya kimliği. EN: Campaign identifier. Architecture: DTO Property.</summary>
    public Guid Id { get; init; }

    /// <summary>TR: Kampanyayı yayınlayan firma. EN: Company publishing the campaign. Architecture: DTO Property.</summary>
    public string Company { get; init; } = string.Empty;

    /// <summary>TR: Kampanya başlığı. EN: Campaign title. Architecture: DTO Property.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>TR: Kampanyaya hak kazanmak için minimum işlem tutarı. EN: Minimum transaction amount required for the campaign. Architecture: DTO Property.</summary>
    public decimal MinimumAmount { get; init; }

    /// <summary>TR: Kampanyanın cashback tutarı. EN: Cashback amount of the campaign. Architecture: DTO Property.</summary>
    public decimal CashbackAmount { get; init; }
}

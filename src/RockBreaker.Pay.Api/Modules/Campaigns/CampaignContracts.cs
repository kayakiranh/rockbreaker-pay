using RockBreaker.Pay.Modules.Wallet.Contracts;

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

/// <summary>
/// TR: Kampanya katılım sonucunu temsil eder.
/// EN: Represents a campaign participation result.
/// Architecture: Response DTO.
/// </summary>
public sealed class CampaignParticipationResponse
{
    /// <summary>TR: Kampanya kimliği. EN: Campaign identifier. Architecture: DTO Property.</summary>
    public Guid CampaignId { get; init; }
    /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: DTO Property.</summary>
    public Guid UserId { get; init; }
    /// <summary>TR: Kullanıcının kampanyaya dahil olup olmadığı. EN: Whether the user is enrolled in the campaign. Architecture: DTO Property.</summary>
    public bool IsJoined { get; init; }
}

/// <summary>
/// TR: Kampanyaya bağlı wallet transferi isteğidir.
/// EN: Wallet transfer request associated with a campaign.
/// Architecture: Request DTO composed over the core transfer contract.
/// </summary>
public sealed class CampaignTransferRequest
{
    /// <summary>TR: Kaynak wallet kimliği. EN: Source wallet identifier. Architecture: DTO Property.</summary>
    public Guid SourceWalletId { get; init; }
    /// <summary>TR: Hedef wallet kimliği. EN: Destination wallet identifier. Architecture: DTO Property.</summary>
    public Guid DestinationWalletId { get; init; }
    /// <summary>TR: Transfer tutarı. EN: Transfer amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: Para birimi. EN: Currency. Architecture: DTO Property.</summary>
    public string Currency { get; init; } = "TRY";

    /// <summary>
    /// TR: Core wallet transfer kontratına explicit mapping yapar.
    /// EN: Explicitly maps to the core wallet-transfer contract.
    /// Architecture: Explicit Mapper; AutoMapper intentionally not used.
    /// </summary>
    public TransferRequest ToTransferRequest() => new()
    {
        SourceWalletId = SourceWalletId,
        DestinationWalletId = DestinationWalletId,
        Amount = Amount,
        Currency = Currency
    };
}

/// <summary>
/// TR: Kampanyaya bağlı transfer sonucunu temsil eder.
/// EN: Represents a campaign-associated transfer result.
/// Architecture: Response DTO.
/// </summary>
public sealed class CampaignTransferResponse
{
    /// <summary>TR: Kampanya kimliği. EN: Campaign identifier. Architecture: DTO Property.</summary>
    public Guid CampaignId { get; init; }
    /// <summary>TR: Kampanyanın potansiyel cashback tutarı. EN: Potential cashback amount defined by the campaign. Architecture: Campaign Metadata.</summary>
    public decimal CampaignCashbackAmount { get; init; }
    /// <summary>TR: Core transfer sonucu. EN: Core transfer result. Architecture: Composed Response DTO.</summary>
    public TransferResponse Transfer { get; init; } = new();
}

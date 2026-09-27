using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.FakeCampaign.Controllers;

/// <summary>
/// TR: Dummy firmaların kampanyalarını ve müşteri katılımlarını simüle eder.
/// EN: Simulates campaigns published by dummy companies and customer participation.
/// Architecture: Fake External Service + In-Memory Repository.
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

    private static readonly ConcurrentDictionary<string, CampaignParticipation> Participations = new();

    /// <summary>
    /// TR: Aktif dummy kampanyaları listeler.
    /// EN: Lists active dummy campaigns.
    /// Architecture: REST Query.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyCollection<Campaign>> GetCampaigns() => Ok(Campaigns);

    /// <summary>
    /// TR: Kullanıcıyı seçilen kampanyaya dahil eder.
    /// EN: Enrolls a user in the selected campaign.
    /// Architecture: REST Command + Idempotent In-Memory Participation Store.
    /// </summary>
    /// <param name="campaignId">TR: Kampanya kimliği. EN: Campaign identifier.</param>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    [HttpPost("{campaignId:guid}/participants/{userId:guid}")]
    public ActionResult<CampaignParticipation> Join(Guid campaignId, Guid userId)
    {
        if (Campaigns.All(x => x.Id != campaignId))
        {
            return NotFound();
        }

        var key = BuildParticipationKey(campaignId, userId);
        var participation = Participations.GetOrAdd(key, _ => new CampaignParticipation
        {
            CampaignId = campaignId,
            UserId = userId,
            JoinedAtUtc = DateTime.UtcNow
        });

        return Ok(participation);
    }

    /// <summary>
    /// TR: Kullanıcının kampanyaya katılım durumunu döndürür.
    /// EN: Returns whether the user participates in the campaign.
    /// Architecture: REST Query + In-Memory Read Model.
    /// </summary>
    /// <param name="campaignId">TR: Kampanya kimliği. EN: Campaign identifier.</param>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    [HttpGet("{campaignId:guid}/participants/{userId:guid}")]
    public ActionResult<CampaignParticipationStatus> GetParticipation(Guid campaignId, Guid userId)
    {
        if (Campaigns.All(x => x.Id != campaignId))
        {
            return NotFound();
        }

        return Ok(new CampaignParticipationStatus
        {
            CampaignId = campaignId,
            UserId = userId,
            IsJoined = Participations.ContainsKey(BuildParticipationKey(campaignId, userId))
        });
    }

    /// <summary>
    /// TR: Kampanya ve kullanıcıdan deterministik participation key üretir.
    /// EN: Builds a deterministic participation key from campaign and user identifiers.
    /// Architecture: In-Memory Repository Key Helper.
    /// </summary>
    private static string BuildParticipationKey(Guid campaignId, Guid userId) =>
        $"{campaignId:N}:{userId:N}";
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

/// <summary>
/// TR: Fake kampanya katılım kaydıdır.
/// EN: Fake campaign participation record.
/// Architecture: In-Memory Entity.
/// </summary>
public sealed class CampaignParticipation
{
    /// <summary>TR: Kampanya kimliği. EN: Campaign identifier. Architecture: Entity Property.</summary>
    public Guid CampaignId { get; init; }
    /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: Entity Property.</summary>
    public Guid UserId { get; init; }
    /// <summary>TR: Katılım UTC zamanı. EN: Participation UTC time. Architecture: Audit Metadata.</summary>
    public DateTime JoinedAtUtc { get; init; }
}

/// <summary>
/// TR: Fake kampanya katılım durumunu temsil eder.
/// EN: Represents fake campaign participation status.
/// Architecture: Response DTO.
/// </summary>
public sealed class CampaignParticipationStatus
{
    /// <summary>TR: Kampanya kimliği. EN: Campaign identifier. Architecture: DTO Property.</summary>
    public Guid CampaignId { get; init; }
    /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: DTO Property.</summary>
    public Guid UserId { get; init; }
    /// <summary>TR: Katılım durumu. EN: Participation state. Architecture: DTO Property.</summary>
    public bool IsJoined { get; init; }
}

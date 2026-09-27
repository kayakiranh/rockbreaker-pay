using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Campaigns;

/// <summary>
/// TR: Kampanya listeleme, katılım ve kampanyaya bağlı transfer endpoint'lerini sunar.
/// EN: Exposes campaign listing, participation and campaign-associated transfer endpoints.
/// Architecture: Thin REST Controller.
/// </summary>
[ApiController]
[Authorize]
[Route("api/campaigns")]
public sealed class CampaignsController : ControllerBase
{
    private readonly ICampaignService _service;

    /// <summary>TR: Controller bağımlılıklarını alır. EN: Receives controller dependencies. Architecture: Constructor Injection.</summary>
    public CampaignsController(ICampaignService service) => _service = service;

    /// <summary>TR: Aktif kampanyaları listeler. EN: Lists active campaigns. Architecture: REST Query.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CampaignResponse>>> GetAsync() =>
        Ok(await _service.GetAsync());

    /// <summary>TR: Aktif kullanıcıyı kampanyaya dahil eder. EN: Enrolls the current user in a campaign. Architecture: REST Command + KYC Guard.</summary>
    [HttpPost("{campaignId:guid}/join")]
    public async Task<IActionResult> JoinAsync(Guid campaignId)
    {
        var result = await _service.JoinAsync(GetUserId(), campaignId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// TR: Kampanya katılımını kontrol edip transferi core wallet transfer zinciri üzerinden çalıştırır.
    /// EN: Validates campaign participation and executes the transfer through the core wallet-transfer chain.
    /// Architecture: REST Command + Use-Case Composition + Fraud/KYC/Ledger/Outbox Integration.
    /// </summary>
    [HttpPost("{campaignId:guid}/transfer")]
    public async Task<IActionResult> TransferAsync(
        Guid campaignId,
        [FromBody] CampaignTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var result = await _service.TransferAsync(
            GetUserId(),
            campaignId,
            request,
            idempotencyKey ?? string.Empty,
            HttpContext.TraceIdentifier);

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: JWT claim içinden authenticated kullanıcı kimliğini döndürür. EN: Returns the authenticated user identifier from the JWT claim. Architecture: Claims-Based Authorization Helper.</summary>
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user id claim is missing."));
}

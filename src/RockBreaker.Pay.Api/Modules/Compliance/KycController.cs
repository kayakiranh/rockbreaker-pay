using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: Kullanıcı KYC başlatma ve yetkili KYC inceleme endpoint'lerini sunar.
/// EN: Exposes user KYC submission and authorized KYC-review endpoints.
/// Architecture: Thin REST Controller + Role-Based Authorization.
/// </summary>
[ApiController]
[Authorize]
[Route("api/kyc")]
public sealed class KycController : ControllerBase
{
    private readonly IKycService _service;

    /// <summary>
    /// TR: Controller bağımlılıklarını alır.
    /// EN: Receives controller dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public KycController(IKycService service) => _service = service;

    /// <summary>TR: Aktif kullanıcının KYC durumunu döndürür. EN: Returns current user's KYC status. Architecture: REST Query.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var result = await _service.GetAsync(GetUserId());
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>TR: Aktif kullanıcının KYC başvurusunu Pending durumuna alır. EN: Submits current user's KYC for review. Architecture: REST Command.</summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit()
    {
        var result = await _service.SubmitAsync(GetUserId());
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Admin/Auditor hedef kullanıcının KYC durumunu görüntüler. EN: Admin/Auditor views a target user's KYC status. Architecture: Protected REST Query.</summary>
    [HttpGet("users/{userId:guid}")]
    [Authorize(Roles = "Admin,Auditor")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var result = await _service.GetAsync(userId);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>TR: Yalnız Admin Pending KYC başvurusunu onaylar veya reddeder. EN: Only Admin approves or rejects a Pending KYC submission. Architecture: Protected REST Command.</summary>
    [HttpPut("users/{userId:guid}/review")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review(
        Guid userId,
        [FromBody] ReviewKycRequest request)
    {
        var result = await _service.ReviewAsync(GetUserId(), userId, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user id claim is missing."));
}

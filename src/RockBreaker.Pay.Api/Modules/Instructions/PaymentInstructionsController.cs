using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Otomatik ödeme talimatı yönetim endpoint'lerini sunar.
/// EN: Exposes automatic-payment-instruction management endpoints.
/// Architecture: Thin REST Controller.
/// </summary>
[ApiController]
[Authorize]
[Route("api/payment-instructions")]
public sealed class PaymentInstructionsController : ControllerBase
{
    private readonly IPaymentInstructionService _service;

    /// <summary>
    /// TR: Controller bağımlılıklarını alır.
    /// EN: Receives controller dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public PaymentInstructionsController(IPaymentInstructionService service) => _service = service;

    /// <summary>TR: Yeni otomatik talimat oluşturur. EN: Creates a new automatic instruction. Architecture: REST Command.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentInstructionRequest request)
    {
        var result = await _service.CreateAsync(GetUserId(), request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Kullanıcının talimatlarını listeler. EN: Lists the user's instructions. Architecture: REST Query.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine() =>
        Ok(await _service.GetMineAsync(GetUserId()));

    /// <summary>TR: Aktif talimatı duraklatır. EN: Pauses an active instruction. Architecture: REST Command.</summary>
    [HttpPost("{instructionId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid instructionId)
    {
        var result = await _service.PauseAsync(GetUserId(), instructionId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Duraklatılmış veya Failed talimatı yeniden aktif eder. EN: Reactivates a paused or failed instruction. Architecture: REST Command.</summary>
    [HttpPost("{instructionId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid instructionId)
    {
        var result = await _service.ResumeAsync(GetUserId(), instructionId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Talimatı iptal eder. EN: Cancels an instruction. Architecture: REST Command.</summary>
    [HttpPost("{instructionId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid instructionId)
    {
        var result = await _service.CancelAsync(GetUserId(), instructionId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// TR: JWT claim içinden aktif kullanıcı kimliğini döndürür.
    /// EN: Returns the current user identifier from the JWT claim.
    /// Architecture: Claims-Based Identity Helper.
    /// </summary>
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user id claim is missing."));
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.MoneyRequests;

/// <summary>
/// TR: Borç/para isteği endpoint'lerini sunar.
/// EN: Exposes money-request endpoints.
/// Architecture: Thin REST Controller.
/// </summary>
[ApiController]
[Authorize]
[Route("api/money-requests")]
public sealed class MoneyRequestsController : ControllerBase
{
    private readonly IMoneyRequestService _service;

    /// <summary>TR: Controller bağımlılıklarını alır. EN: Receives controller dependencies. Architecture: Constructor Injection.</summary>
    public MoneyRequestsController(IMoneyRequestService service) => _service = service;

    /// <summary>TR: Yeni para isteği oluşturur. EN: Creates a new money request. Architecture: REST Command.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMoneyRequest request)
    {
        var result = await _service.CreateAsync(GetUserId(), request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Kullanıcının para isteklerini listeler. EN: Lists the user's money requests. Architecture: REST Query.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine() => Ok(await _service.GetMineAsync(GetUserId()));

    /// <summary>TR: Para isteğini kabul eder. EN: Accepts a money request. Architecture: REST Command.</summary>
    [HttpPost("{requestId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid requestId)
    {
        var result = await _service.AcceptAsync(GetUserId(), requestId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Para isteğini reddeder. EN: Rejects a money request. Architecture: REST Command.</summary>
    [HttpPost("{requestId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid requestId)
    {
        var result = await _service.RejectAsync(GetUserId(), requestId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Para isteğini iptal eder. EN: Cancels a money request. Architecture: REST Command.</summary>
    [HttpPost("{requestId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid requestId)
    {
        var result = await _service.CancelAsync(GetUserId(), requestId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user id claim is missing."));
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.Wallet.Controllers;

/// <summary>
/// TR: Wallet oluşturma, özet, hareket ve limit endpoint'lerini sunar.
/// EN: Exposes wallet creation, summary, movement and limit endpoints.
/// Architecture: Thin REST Controller.
/// </summary>
[ApiController]
[Authorize]
[Route("api/wallets")]
public sealed class WalletsController : ControllerBase
{
    private readonly IWalletService _service;

    /// <summary>TR: Controller bağımlılıklarını alır. EN: Receives controller dependencies. Architecture: Constructor Injection.</summary>
    /// <param name="service">TR: Wallet application service. EN: Wallet application service.</param>
    public WalletsController(IWalletService service) => _service = service;

    /// <summary>TR: Aktif kullanıcı için wallet oluşturur. EN: Creates a wallet for the current user. Architecture: REST Command.</summary>
    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var result = await _service.CreateAsync(GetUserId());
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Aktif kullanıcının wallet özetini döndürür. EN: Returns current user's wallet summary. Architecture: REST Query.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> Get()
    {
        var result = await _service.GetAsync(GetUserId());
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>TR: Aktif kullanıcının wallet hareketlerini döndürür. EN: Returns current user's wallet movements. Architecture: REST Query.</summary>
    [HttpGet("me/movements")]
    public async Task<IActionResult> GetMovements()
    {
        var result = await _service.GetMovementsAsync(GetUserId());
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>TR: Aktif kullanıcının wallet limitlerini günceller. EN: Updates current user's wallet limits. Architecture: REST Command.</summary>
    /// <param name="request">TR: Yeni limitler. EN: New limits.</param>
    [HttpPut("me/limits")]
    public async Task<IActionResult> UpdateLimits([FromBody] UpdateWalletLimitsRequest request)
    {
        var result = await _service.UpdateLimitsAsync(GetUserId(), request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user id claim is missing."));
}

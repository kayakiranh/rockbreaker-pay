using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RockBreaker.Pay.Modules.Identity.Application;
using RockBreaker.Pay.Modules.Identity.Contracts;

namespace RockBreaker.Pay.Modules.Identity.Controllers;

/// <summary>
/// TR: Giriş yapmış kullanıcının profil endpoint'lerini sunar.
/// EN: Exposes profile endpoints for the authenticated user.
/// Architecture: Thin REST Controller + Claims-Based Identity.
/// </summary>
[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController : ControllerBase
{
    private readonly IIdentityService _service;

    /// <summary>TR: Controller bağımlılıklarını alır. EN: Receives controller dependencies. Architecture: Constructor Injection.</summary>
    /// <param name="service">TR: Identity service. EN: Identity service.</param>
    public ProfileController(IIdentityService service) => _service = service;

    /// <summary>TR: Aktif kullanıcının profilini döndürür. EN: Returns current user's profile. Architecture: REST Query.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _service.GetProfileAsync(GetUserId());
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>TR: Aktif kullanıcının profilini günceller. EN: Updates current user's profile. Architecture: REST Command.</summary>
    /// <param name="request">TR: Profil alanları. EN: Profile fields.</param>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
    {
        var result = await _service.UpdateProfileAsync(GetUserId(), request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user id claim is missing."));
}

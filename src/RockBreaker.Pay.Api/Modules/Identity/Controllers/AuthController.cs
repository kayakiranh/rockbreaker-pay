using Microsoft.AspNetCore.Mvc;
using RockBreaker.Pay.Modules.Identity.Application;
using RockBreaker.Pay.Modules.Identity.Contracts;

namespace RockBreaker.Pay.Modules.Identity.Controllers;

/// <summary>
/// TR: Register, login, refresh-token ve password reset endpoint'lerini sunar.
/// EN: Exposes registration, login, refresh-token and password-reset endpoints.
/// Architecture: Thin REST Controller.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IIdentityService _service;

    /// <summary>TR: Controller bağımlılıklarını alır. EN: Receives controller dependencies. Architecture: Constructor Injection.</summary>
    /// <param name="service">TR: Identity service. EN: Identity service.</param>
    public AuthController(IIdentityService service) => _service = service;

    /// <summary>TR: Kullanıcı kaydeder. EN: Registers a user. Architecture: REST Command.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _service.RegisterAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Kullanıcı girişi yapar. EN: Logs a user in. Architecture: REST Command.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _service.LoginAsync(request);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    /// <summary>TR: Refresh token ile token çiftini yeniler. EN: Rotates tokens using a refresh token. Architecture: REST Command + Token Rotation.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _service.RefreshAsync(request);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    /// <summary>TR: Password reset token üretir. EN: Creates a password-reset token. Architecture: REST Command.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request) =>
        Ok(await _service.ForgotPasswordAsync(request));

    /// <summary>TR: Reset token ile parolayı değiştirir. EN: Changes password using a reset token. Architecture: REST Command.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _service.ResetPasswordAsync(request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

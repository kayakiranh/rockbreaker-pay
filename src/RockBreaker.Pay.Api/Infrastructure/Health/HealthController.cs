using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Infrastructure.Health;

/// <summary>
/// TR: Container/orchestrator ve operasyon ekipleri için live/readiness endpoint'lerini sunar.
/// EN: Exposes live/readiness endpoints for containers, orchestrators and operations teams.
/// Architecture: Health Probe Controller.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly IReadinessService _readinessService;

    /// <summary>
    /// TR: Controller bağımlılıklarını alır.
    /// EN: Receives controller dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="readinessService">TR: Readiness servisi. EN: Readiness service.</param>
    public HealthController(IReadinessService readinessService) => _readinessService = readinessService;

    /// <summary>
    /// TR: Process çalışıyorsa 200 döner; harici bağımlılıkları kontrol etmez.
    /// EN: Returns 200 when the process is alive; external dependencies are not checked.
    /// Architecture: Liveness Probe.
    /// </summary>
    /// <returns>TR: Live durumu. EN: Liveness state.</returns>
    [HttpGet("live")]
    public ActionResult<object> Live() => Ok(new { status = "Healthy" });

    /// <summary>
    /// TR: MSSQL ve Elasticsearch hazırsa 200, değilse 503 döner.
    /// EN: Returns 200 when MSSQL and Elasticsearch are ready, otherwise 503.
    /// Architecture: Readiness Probe.
    /// </summary>
    /// <returns>TR: Readiness sonucu. EN: Readiness result.</returns>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var result = await _readinessService.CheckAsync();
        return result.Status == "Healthy"
            ? Ok(result)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}

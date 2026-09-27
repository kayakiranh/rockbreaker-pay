using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Reporting;

/// <summary>
/// TR: Resmi kurum talebi için örnek konsolide transaction/fraud/audit rapor endpoint'ini sunar.
/// EN: Exposes an example consolidated transaction/fraud/audit report endpoint for an official authority request.
/// Architecture: Role-Protected Thin REST Controller.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Auditor")]
[Route("api/reports/government")]
public sealed class GovernmentReportController : ControllerBase
{
    private readonly IGovernmentReportService _service;

    /// <summary>TR: Controller bağımlılığını alır. EN: Receives the controller dependency. Architecture: Constructor Injection.</summary>
    public GovernmentReportController(IGovernmentReportService service) => _service = service;

    /// <summary>
    /// TR: Tarih aralığı ve opsiyonel wallet filtresi için resmi kurum örnek raporunu döndürür.
    /// EN: Returns the example official-authority report for a date range and optional wallet filter.
    /// Architecture: Regulatory Composite Query.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<GovernmentReportResponse>> Get([FromQuery] ReportQuery query) =>
        Ok(await _service.BuildAsync(query));
}

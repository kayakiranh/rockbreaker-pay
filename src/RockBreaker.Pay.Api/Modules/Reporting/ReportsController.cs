using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Reporting;

/// <summary>
/// TR: Yalnız Admin ve Auditor rollerinin erişebildiği regülasyon rapor endpoint'lerini sunar.
/// EN: Exposes regulatory report endpoints accessible only to Admin and Auditor roles.
/// Architecture: Role-Protected Thin REST Controller.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Auditor")]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IRegulatoryReportService _service;

    /// <summary>
    /// TR: Controller bağımlılıklarını alır.
    /// EN: Receives controller dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="service">TR: Rapor servisi. EN: Report service.</param>
    public ReportsController(IRegulatoryReportService service) => _service = service;

    /// <summary>
    /// TR: Finansal transaction raporunu JSON döndürür.
    /// EN: Returns the financial transaction report as JSON.
    /// Architecture: Regulatory REST Query.
    /// </summary>
    /// <param name="query">TR: Rapor filtresi. EN: Report filter.</param>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] ReportQuery query) =>
        Ok(await _service.GetTransactionsAsync(query));

    /// <summary>
    /// TR: Fraud değerlendirme raporunu JSON döndürür.
    /// EN: Returns the fraud evaluation report as JSON.
    /// Architecture: Regulatory REST Query.
    /// </summary>
    /// <param name="query">TR: Rapor filtresi. EN: Report filter.</param>
    [HttpGet("fraud")]
    public async Task<IActionResult> GetFraud([FromQuery] ReportQuery query) =>
        Ok(await _service.GetFraudEventsAsync(query));

    /// <summary>
    /// TR: Maskelenmiş HTTP audit raporunu JSON döndürür.
    /// EN: Returns the masked HTTP audit report as JSON.
    /// Architecture: Regulatory REST Query.
    /// </summary>
    /// <param name="query">TR: Rapor filtresi. EN: Report filter.</param>
    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit([FromQuery] ReportQuery query) =>
        Ok(await _service.GetAuditLogsAsync(query));

    /// <summary>
    /// TR: Finansal transaction raporunu UTF-8 CSV dosyası olarak üretir.
    /// EN: Produces the financial transaction report as a UTF-8 CSV file.
    /// Architecture: Regulatory File Export.
    /// </summary>
    /// <param name="query">TR: Rapor filtresi. EN: Report filter.</param>
    [HttpGet("transactions.csv")]
    public async Task<IActionResult> ExportTransactionsCsv([FromQuery] ReportQuery query) =>
        File(await _service.ExportTransactionsCsvAsync(query), "text/csv; charset=utf-8", "transactions.csv");

    /// <summary>
    /// TR: Fraud raporunu UTF-8 CSV dosyası olarak üretir.
    /// EN: Produces the fraud report as a UTF-8 CSV file.
    /// Architecture: Regulatory File Export.
    /// </summary>
    /// <param name="query">TR: Rapor filtresi. EN: Report filter.</param>
    [HttpGet("fraud.csv")]
    public async Task<IActionResult> ExportFraudCsv([FromQuery] ReportQuery query) =>
        File(await _service.ExportFraudCsvAsync(query), "text/csv; charset=utf-8", "fraud.csv");

    /// <summary>
    /// TR: HTTP audit raporunu UTF-8 CSV dosyası olarak üretir.
    /// EN: Produces the HTTP audit report as a UTF-8 CSV file.
    /// Architecture: Regulatory File Export.
    /// </summary>
    /// <param name="query">TR: Rapor filtresi. EN: Report filter.</param>
    [HttpGet("audit.csv")]
    public async Task<IActionResult> ExportAuditCsv([FromQuery] ReportQuery query) =>
        File(await _service.ExportAuditCsvAsync(query), "text/csv; charset=utf-8", "audit.csv");
}

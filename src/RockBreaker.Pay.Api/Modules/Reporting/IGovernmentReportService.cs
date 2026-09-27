namespace RockBreaker.Pay.Modules.Reporting;

/// <summary>
/// TR: Resmi kurum için örnek konsolide regülasyon raporunu üretir.
/// EN: Produces an example consolidated regulatory report for an official authority.
/// Architecture: Composite Query Service Interface.
/// </summary>
public interface IGovernmentReportService
{
    /// <summary>TR: İstenen tarih/wallet filtresine göre resmi kurum raporunu oluşturur. EN: Builds the official-authority report for the requested date/wallet filter. Architecture: Composite Query.</summary>
    Task<GovernmentReportResponse> BuildAsync(ReportQuery query);
}

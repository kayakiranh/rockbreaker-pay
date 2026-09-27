namespace RockBreaker.Pay.Modules.Reporting;

/// <summary>
/// TR: Regülasyon/denetim amaçlı transaction, fraud ve HTTP audit raporlarını üretir.
/// EN: Produces transaction, fraud and HTTP-audit reports for regulatory/audit purposes.
/// Architecture: Application Query Service.
/// </summary>
public interface IRegulatoryReportService
{
    /// <summary>TR: Finansal transaction raporu üretir. EN: Produces a financial transaction report. Architecture: Query Service.</summary>
    Task<IReadOnlyCollection<TransactionReportRow>> GetTransactionsAsync(ReportQuery query);

    /// <summary>TR: Fraud değerlendirme raporu üretir. EN: Produces a fraud-evaluation report. Architecture: Query Service.</summary>
    Task<IReadOnlyCollection<FraudReportRow>> GetFraudEventsAsync(ReportQuery query);

    /// <summary>TR: HTTP audit raporu üretir. EN: Produces an HTTP audit report. Architecture: Query Service.</summary>
    Task<IReadOnlyCollection<AuditReportRow>> GetAuditLogsAsync(ReportQuery query);

    /// <summary>TR: Transaction raporunu UTF-8 CSV olarak üretir. EN: Produces the transaction report as UTF-8 CSV. Architecture: Report Exporter.</summary>
    Task<byte[]> ExportTransactionsCsvAsync(ReportQuery query);

    /// <summary>TR: Fraud raporunu UTF-8 CSV olarak üretir. EN: Produces the fraud report as UTF-8 CSV. Architecture: Report Exporter.</summary>
    Task<byte[]> ExportFraudCsvAsync(ReportQuery query);

    /// <summary>TR: Audit raporunu UTF-8 CSV olarak üretir. EN: Produces the audit report as UTF-8 CSV. Architecture: Report Exporter.</summary>
    Task<byte[]> ExportAuditCsvAsync(ReportQuery query);
}

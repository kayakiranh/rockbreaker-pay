using RockBreaker.Pay.Modules.Fraud;

namespace RockBreaker.Pay.Modules.Reporting;

/// <summary>
/// TR: Mevcut regulator-grade transaction, fraud ve audit read modellerini tek resmi kurum raporunda birleştirir.
/// EN: Combines existing regulator-grade transaction, fraud and audit read models into one official-authority report.
/// Architecture: Composite Query Service; MSSQL remains the source of truth.
/// </summary>
public sealed class GovernmentReportService : IGovernmentReportService
{
    private readonly IRegulatoryReportService _regulatoryReportService;

    /// <summary>TR: Rapor servisi bağımlılığını alır. EN: Receives the report-service dependency. Architecture: Constructor Injection.</summary>
    public GovernmentReportService(IRegulatoryReportService regulatoryReportService) =>
        _regulatoryReportService = regulatoryReportService;

    /// <inheritdoc />
    public async Task<GovernmentReportResponse> BuildAsync(ReportQuery query)
    {
        var transactions = await _regulatoryReportService.GetTransactionsAsync(query);
        var fraudEvents = await _regulatoryReportService.GetFraudEventsAsync(query);
        var auditLogs = await _regulatoryReportService.GetAuditLogsAsync(query);
        var failedAudits = auditLogs.Where(x => !x.IsSuccess).ToArray();

        return new GovernmentReportResponse
        {
            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,
            GeneratedAtUtc = DateTime.UtcNow,
            TransactionCount = transactions.Count,
            TotalTransactionAmount = transactions.Sum(x => x.Amount),
            FraudEvaluationCount = fraudEvents.Count,
            FraudRejectedCount = fraudEvents.Count(x => x.Action == (int)FraudAction.Reject),
            FraudBlockedWalletCount = fraudEvents.Count(x => x.Action == (int)FraudAction.BlockWallet),
            AuditRequestCount = auditLogs.Count,
            FailedAuditRequestCount = failedAudits.Length,
            Transactions = transactions,
            FraudEvents = fraudEvents,
            FailedAuditRequests = failedAudits
        };
    }
}

namespace RockBreaker.Pay.Modules.Reporting;

/// <summary>
/// TR: Resmi kurum talebi için transaction, fraud ve audit özetini tek raporda birleştiren örnek response modelidir.
/// EN: Example response model combining transaction, fraud and audit summaries for an official authority request.
/// Architecture: Regulatory Composite Read Model.
/// </summary>
public sealed class GovernmentReportResponse
{
    /// <summary>TR: Rapor başlangıcı. EN: Report start. Architecture: Report Metadata.</summary>
    public DateTime FromUtc { get; init; }
    /// <summary>TR: Rapor bitişi. EN: Report end. Architecture: Report Metadata.</summary>
    public DateTime ToUtc { get; init; }
    /// <summary>TR: Rapor üretim zamanı. EN: Report generation time. Architecture: Audit Metadata.</summary>
    public DateTime GeneratedAtUtc { get; init; }
    /// <summary>TR: Finansal işlem sayısı. EN: Financial transaction count. Architecture: Summary Metric.</summary>
    public int TransactionCount { get; init; }
    /// <summary>TR: Toplam işlem tutarı. EN: Total transaction amount. Architecture: Summary Metric.</summary>
    public decimal TotalTransactionAmount { get; init; }
    /// <summary>TR: Fraud değerlendirme sayısı. EN: Fraud evaluation count. Architecture: Summary Metric.</summary>
    public int FraudEvaluationCount { get; init; }
    /// <summary>TR: Fraud nedeniyle reddedilen işlem sayısı. EN: Count of transactions rejected for fraud. Architecture: Risk Metric.</summary>
    public int FraudRejectedCount { get; init; }
    /// <summary>TR: Fraud nedeniyle bloke edilen wallet kararı sayısı. EN: Count of wallet-block decisions caused by fraud. Architecture: Risk Metric.</summary>
    public int FraudBlockedWalletCount { get; init; }
    /// <summary>TR: HTTP audit kayıt sayısı. EN: HTTP audit record count. Architecture: Audit Metric.</summary>
    public int AuditRequestCount { get; init; }
    /// <summary>TR: Başarısız HTTP audit kayıt sayısı. EN: Failed HTTP audit record count. Architecture: Audit Metric.</summary>
    public int FailedAuditRequestCount { get; init; }
    /// <summary>TR: İşlem detayları. EN: Transaction details. Architecture: Regulatory Detail Collection.</summary>
    public IReadOnlyCollection<TransactionReportRow> Transactions { get; init; } = Array.Empty<TransactionReportRow>();
    /// <summary>TR: Fraud detayları. EN: Fraud details. Architecture: Regulatory Detail Collection.</summary>
    public IReadOnlyCollection<FraudReportRow> FraudEvents { get; init; } = Array.Empty<FraudReportRow>();
    /// <summary>TR: Başarısız HTTP çağrılarının maskelenmiş audit detayları. EN: Masked audit details for failed HTTP calls. Architecture: Privacy-Aware Regulatory Detail.</summary>
    public IReadOnlyCollection<AuditReportRow> FailedAuditRequests { get; init; } = Array.Empty<AuditReportRow>();
}

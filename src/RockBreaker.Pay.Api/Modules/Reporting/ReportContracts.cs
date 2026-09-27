namespace RockBreaker.Pay.Modules.Reporting;

/// <summary>
/// TR: Regülasyon raporlarının ortak tarih ve filtre parametrelerini taşır.
/// EN: Carries common date and filter parameters for regulatory reports.
/// Architecture: Query DTO.
/// </summary>
public sealed class ReportQuery
{
    /// <summary>TR: Rapor başlangıç UTC zamanı. EN: Report start UTC time. Architecture: Query Property.</summary>
    public DateTime FromUtc { get; init; }

    /// <summary>TR: Rapor bitiş UTC zamanı. EN: Report end UTC time. Architecture: Query Property.</summary>
    public DateTime ToUtc { get; init; }

    /// <summary>TR: Opsiyonel wallet filtresi. EN: Optional wallet filter. Architecture: Query Property.</summary>
    public Guid? WalletId { get; init; }
}

/// <summary>
/// TR: Finansal transaction rapor satırını temsil eder.
/// EN: Represents a financial transaction report row.
/// Architecture: Regulatory Read Model.
/// </summary>
public sealed class TransactionReportRow
{
    /// <summary>TR: İşlem kimliği. EN: Transaction identifier. Architecture: Report Property.</summary>
    public Guid TransactionId { get; init; }

    /// <summary>TR: Kaynak wallet. EN: Source wallet. Architecture: Report Property.</summary>
    public Guid? SourceWalletId { get; init; }

    /// <summary>TR: Hedef wallet. EN: Destination wallet. Architecture: Report Property.</summary>
    public Guid? DestinationWalletId { get; init; }

    /// <summary>TR: Tutar. EN: Amount. Architecture: Report Property.</summary>
    public decimal Amount { get; init; }

    /// <summary>TR: Para birimi. EN: Currency. Architecture: Report Property.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>TR: İşlem tipi. EN: Transaction type. Architecture: Report Property.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>TR: İşlem durum kodu. EN: Transaction status code. Architecture: Report Property.</summary>
    public int Status { get; init; }

    /// <summary>TR: Idempotency anahtarı. EN: Idempotency key. Architecture: Report Property.</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>TR: Correlation kimliği. EN: Correlation identifier. Architecture: Report Property.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>TR: UTC oluşturulma zamanı. EN: UTC creation time. Architecture: Report Property.</summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>TR: UTC tamamlanma zamanı. EN: UTC completion time. Architecture: Report Property.</summary>
    public DateTime? CompletedAtUtc { get; init; }
}

/// <summary>
/// TR: Fraud değerlendirme rapor satırını temsil eder.
/// EN: Represents a fraud-evaluation report row.
/// Architecture: Regulatory Read Model.
/// </summary>
public sealed class FraudReportRow
{
    /// <summary>TR: Fraud event kimliği. EN: Fraud event identifier. Architecture: Report Property.</summary>
    public Guid Id { get; init; }

    /// <summary>TR: Wallet kimliği. EN: Wallet identifier. Architecture: Report Property.</summary>
    public Guid WalletId { get; init; }

    /// <summary>TR: Değerlendirilen tutar. EN: Evaluated amount. Architecture: Report Property.</summary>
    public decimal Amount { get; init; }

    /// <summary>TR: Risk skoru. EN: Risk score. Architecture: Report Property.</summary>
    public int RiskScore { get; init; }

    /// <summary>TR: Fraud aksiyon kodu. EN: Fraud action code. Architecture: Report Property.</summary>
    public int Action { get; init; }

    /// <summary>TR: Tetiklenen kural kodlarının JSON listesi. EN: JSON list of triggered rule codes. Architecture: Explainability Property.</summary>
    public string TriggeredRulesJson { get; init; } = string.Empty;

    /// <summary>TR: UTC değerlendirme zamanı. EN: UTC evaluation time. Architecture: Report Property.</summary>
    public DateTime CreatedAtUtc { get; init; }
}

/// <summary>
/// TR: HTTP audit rapor satırını temsil eder.
/// EN: Represents an HTTP audit report row.
/// Architecture: Regulatory Audit Read Model.
/// </summary>
public sealed class AuditReportRow
{
    /// <summary>TR: Audit sıra kimliği. EN: Audit sequence identifier. Architecture: Report Property.</summary>
    public long Id { get; init; }

    /// <summary>TR: Correlation kimliği. EN: Correlation identifier. Architecture: Report Property.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>TR: Trace kimliği. EN: Trace identifier. Architecture: Report Property.</summary>
    public string? TraceId { get; init; }

    /// <summary>TR: HTTP metodu. EN: HTTP method. Architecture: Report Property.</summary>
    public string HttpMethod { get; init; } = string.Empty;

    /// <summary>TR: HTTP path. EN: HTTP path. Architecture: Report Property.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>TR: Maskelenmiş request body. EN: Masked request body. Architecture: Privacy-Aware Report Property.</summary>
    public string? RequestBody { get; init; }

    /// <summary>TR: Maskelenmiş response body. EN: Masked response body. Architecture: Privacy-Aware Report Property.</summary>
    public string? ResponseBody { get; init; }

    /// <summary>TR: HTTP status kodu. EN: HTTP status code. Architecture: Report Property.</summary>
    public int ResponseStatusCode { get; init; }

    /// <summary>TR: Response süresi milisaniye. EN: Response duration in milliseconds. Architecture: Report Property.</summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>TR: Başarı bilgisi. EN: Success flag. Architecture: Report Property.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>TR: İstemci IP adresi. EN: Client IP address. Architecture: Security Audit Property.</summary>
    public string? ClientIp { get; init; }

    /// <summary>TR: User-Agent bilgisi. EN: User-Agent value. Architecture: Security Audit Property.</summary>
    public string? UserAgent { get; init; }

    /// <summary>TR: UTC audit zamanı. EN: UTC audit time. Architecture: Report Property.</summary>
    public DateTime CreatedAtUtc { get; init; }
}

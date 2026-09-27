namespace RockBreaker.Pay.Infrastructure.Auditing;

/// <summary>
/// TR: Regülasyon ve operasyon incelemeleri için HTTP isteğinin denetlenebilir kaydını temsil eder.
/// EN: Represents an auditable HTTP request record for regulatory and operational investigations.
/// Architecture: Append-Only Audit Record.
/// </summary>
public sealed class AuditLogRecord
{
    /// <summary>TR: Uçtan uca correlation kimliği. EN: End-to-end correlation identifier. Architecture: Observability Metadata.</summary>
    public string CorrelationId { get; init; } = string.Empty;
    /// <summary>TR: Activity trace kimliği. EN: Activity trace identifier. Architecture: Distributed Tracing Metadata.</summary>
    public string? TraceId { get; init; }
    /// <summary>TR: HTTP metodu. EN: HTTP method. Architecture: Audit Metadata.</summary>
    public string HttpMethod { get; init; } = string.Empty;
    /// <summary>TR: İstek path'i. EN: Request path. Architecture: Audit Metadata.</summary>
    public string Path { get; init; } = string.Empty;
    /// <summary>TR: Hassas alanları maskelenmiş request body. EN: Request body with sensitive fields masked. Architecture: Privacy-Aware Audit.</summary>
    public string? RequestBody { get; init; }
    /// <summary>TR: Hassas alanları maskelenmiş response body. EN: Response body with sensitive fields masked. Architecture: Privacy-Aware Audit.</summary>
    public string? ResponseBody { get; init; }
    /// <summary>TR: HTTP status code. EN: HTTP status code. Architecture: Audit Outcome.</summary>
    public int ResponseStatusCode { get; init; }
    /// <summary>TR: İstek süresi milisaniye. EN: Request duration in milliseconds. Architecture: Performance Telemetry.</summary>
    public long ResponseTimeMs { get; init; }
    /// <summary>TR: HTTP sonucunun başarılı olup olmadığını belirtir. EN: Indicates whether the HTTP outcome is successful. Architecture: Audit Outcome.</summary>
    public bool IsSuccess { get; init; }
    /// <summary>TR: İstemci IP adresi. EN: Client IP address. Architecture: Security Audit Metadata.</summary>
    public string? ClientIp { get; init; }
    /// <summary>TR: User-Agent bilgisi. EN: User-Agent value. Architecture: Security Audit Metadata.</summary>
    public string? UserAgent { get; init; }
    /// <summary>TR: UTC kayıt zamanı. EN: UTC record time. Architecture: Immutable Audit Timeline.</summary>
    public DateTime CreatedAtUtc { get; init; }
}

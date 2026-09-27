namespace RockBreaker.Pay.Infrastructure.Health;

/// <summary>
/// TR: Uygulamanın live/readiness sağlık durumunu dış API'ye taşır.
/// EN: Carries application live/readiness health state to the external API.
/// Architecture: Health Check Response DTO.
/// </summary>
public sealed class HealthStatusResponse
{
    /// <summary>TR: Genel sağlık durumu. EN: Overall health state. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>TR: MSSQL bağlantı durumu. EN: MSSQL connectivity state. Architecture: Dependency Health Property.</summary>
    public string Database { get; init; } = string.Empty;

    /// <summary>TR: Elasticsearch bağlantı durumu. EN: Elasticsearch connectivity state. Architecture: Dependency Health Property.</summary>
    public string Elasticsearch { get; init; } = string.Empty;
}

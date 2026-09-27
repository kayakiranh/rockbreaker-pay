namespace RockBreaker.Pay.Modules.CustomerJourney;

/// <summary>
/// TR: Bir business case'in uçtan uca başarılı müşteri journey dokümanını temsil eder.
/// EN: Represents the end-to-end successful customer journey document for a business case.
/// Architecture: Developer Experience Response DTO.
/// </summary>
public sealed class CustomerJourneyResponse
{
    /// <summary>TR: Journey başlığı. EN: Journey title. Architecture: DTO Property.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>TR: Akışın başarı ön koşulları ve önemli notları. EN: Success prerequisites and important flow notes. Architecture: DTO Property.</summary>
    public IReadOnlyCollection<string> Notes { get; init; } = Array.Empty<string>();

    /// <summary>TR: Sıralı success adımları. EN: Ordered success steps. Architecture: DTO Property.</summary>
    public IReadOnlyCollection<CustomerJourneyStep> Steps { get; init; } = Array.Empty<CustomerJourneyStep>();
}

/// <summary>
/// TR: Journey içindeki tek endpoint/işlem adımını ve success request örneğini temsil eder.
/// EN: Represents one endpoint/operation step and successful request example in a journey.
/// Architecture: Developer Experience Step DTO.
/// </summary>
public sealed class CustomerJourneyStep
{
    /// <summary>TR: Çalıştırma sırası. EN: Execution order. Architecture: DTO Property.</summary>
    public int Order { get; init; }

    /// <summary>TR: Adım adı. EN: Step name. Architecture: DTO Property.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>TR: Servis veya entegrasyon zinciri. EN: Service or integration chain. Architecture: DTO Property.</summary>
    public string Service { get; init; } = string.Empty;

    /// <summary>TR: HTTP metodu veya INTERNAL. EN: HTTP method or INTERNAL. Architecture: DTO Property.</summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>TR: Endpoint veya internal component. EN: Endpoint or internal component. Architecture: DTO Property.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>TR: Bearer token gerekip gerekmediği. EN: Whether a bearer token is required. Architecture: Security Metadata.</summary>
    public bool RequiresBearerToken { get; init; }

    /// <summary>TR: Request header örnekleri. EN: Example request headers. Architecture: Documentation Property.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    /// <summary>TR: Success request body örneği. EN: Successful request-body example. Architecture: Documentation Property.</summary>
    public object? RequestBody { get; init; }

    /// <summary>TR: Beklenen başarılı sonuç. EN: Expected successful result. Architecture: Journey Metadata.</summary>
    public string ExpectedSuccess { get; init; } = string.Empty;

    /// <summary>TR: Adımda devreye giren güvenlik, finans ve entegrasyon bileşenleri. EN: Security, financial and integration components involved in the step. Architecture: Integration Metadata.</summary>
    public IReadOnlyCollection<string> Integrations { get; init; } = Array.Empty<string>();

    /// <summary>TR: Kullanılan temel mimari/pattern bilgisi. EN: Core architecture/pattern information. Architecture: Documentation Metadata.</summary>
    public string Architecture { get; init; } = string.Empty;
}

/// <summary>
/// TR: Journey controller'larında ortak kullanılan örnek HTTP header'larını üretir.
/// EN: Creates example HTTP headers shared by journey controllers.
/// Architecture: Documentation Helper.
/// </summary>
public static class JourneyHeaders
{
    /// <summary>TR: Bearer token ve JSON header'larını üretir. EN: Creates bearer-token and JSON headers. Architecture: Documentation Helper.</summary>
    public static Dictionary<string, string> Bearer() => new()
    {
        ["Authorization"] = "Bearer <access-token-from-register-or-login>",
        ["Content-Type"] = "application/json"
    };

    /// <summary>TR: Bearer token, JSON ve idempotency header'larını üretir. EN: Creates bearer-token, JSON and idempotency headers. Architecture: Documentation Helper.</summary>
    public static Dictionary<string, string> Financial(string idempotencyKey)
    {
        var headers = Bearer();
        headers["Idempotency-Key"] = idempotencyKey;
        return headers;
    }
}

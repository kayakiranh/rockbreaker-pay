namespace RockBreaker.Pay.Infrastructure.Health;

/// <summary>
/// TR: Uygulamanın kritik bağımlılıklarının readiness durumunu kontrol eder.
/// EN: Checks readiness of the application's critical dependencies.
/// Architecture: Dependency Health Service Interface.
/// </summary>
public interface IReadinessService
{
    /// <summary>
    /// TR: MSSQL ve Elasticsearch bağlantılarını kontrol eder.
    /// EN: Checks MSSQL and Elasticsearch connectivity.
    /// Architecture: Readiness Probe Service.
    /// </summary>
    /// <returns>TR: Sağlık durumu. EN: Health status.</returns>
    Task<HealthStatusResponse> CheckAsync();
}

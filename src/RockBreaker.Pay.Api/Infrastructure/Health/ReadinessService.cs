using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;

namespace RockBreaker.Pay.Infrastructure.Health;

/// <summary>
/// TR: MSSQL'e gerçek sorgu ve Elasticsearch'e gerçek HTTP isteği yaparak readiness durumunu hesaplar.
/// EN: Calculates readiness by executing a real MSSQL query and a real Elasticsearch HTTP request.
/// Architecture: Dependency Health Service.
/// </summary>
public sealed class ReadinessService : IReadinessService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// TR: Readiness service bağımlılıklarını alır.
    /// EN: Receives readiness-service dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    /// <param name="httpClientFactory">TR: HTTP client factory. EN: HTTP client factory.</param>
    public ReadinessService(
        IDbConnectionFactory connectionFactory,
        IHttpClientFactory httpClientFactory)
    {
        _connectionFactory = connectionFactory;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<HealthStatusResponse> CheckAsync()
    {
        var databaseHealthy = await CheckDatabaseAsync();
        var elasticsearchHealthy = await CheckElasticsearchAsync();

        return new HealthStatusResponse
        {
            Status = databaseHealthy && elasticsearchHealthy ? "Healthy" : "Unhealthy",
            Database = databaseHealthy ? "Healthy" : "Unhealthy",
            Elasticsearch = elasticsearchHealthy ? "Healthy" : "Unhealthy"
        };
    }

    /// <summary>
    /// TR: MSSQL üzerinde SELECT 1 çalıştırarak bağlantı durumunu kontrol eder.
    /// EN: Checks MSSQL connectivity by executing SELECT 1.
    /// Architecture: Active Dependency Probe.
    /// </summary>
    /// <returns>TR: DB sağlıklı ise true. EN: True when the database is healthy.</returns>
    private async Task<bool> CheckDatabaseAsync()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT 1;") == 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// TR: Elasticsearch root endpoint'ine kısa timeout ile istek gönderir.
    /// EN: Sends a short-timeout request to the Elasticsearch root endpoint.
    /// Architecture: Active Dependency Probe.
    /// </summary>
    /// <returns>TR: Elasticsearch erişilebilir ise true. EN: True when Elasticsearch is reachable.</returns>
    private async Task<bool> CheckElasticsearchAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ElasticsearchHealth");
            using var response = await client.GetAsync(string.Empty);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

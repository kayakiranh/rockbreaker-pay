using System.Net.Http.Json;

namespace RockBreaker.Pay.Infrastructure.Auditing;

/// <summary>
/// TR: Audit kayıtlarını Elasticsearch HTTP API üzerinden indeksler.
/// EN: Indexes audit records through the Elasticsearch HTTP API.
/// Architecture: HTTP Adapter for a searchable audit projection.
/// </summary>
public sealed class ElasticAuditWriter : IElasticAuditWriter
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ElasticAuditWriter> _logger;

    /// <summary>
    /// TR: Elasticsearch adapter bağımlılıklarını alır.
    /// EN: Receives Elasticsearch adapter dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="httpClient">TR: Elasticsearch HTTP istemcisi. EN: Elasticsearch HTTP client.</param>
    /// <param name="logger">TR: Uygulama logger'ı. EN: Application logger.</param>
    public ElasticAuditWriter(HttpClient httpClient, ILogger<ElasticAuditWriter> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task WriteAsync(AuditLogRecord record)
    {
        try
        {
            var index = $"rockbreaker-pay-audit-{DateTime.UtcNow:yyyy.MM}";
            using var response = await _httpClient.PostAsJsonAsync($"{index}/_doc", record);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Elasticsearch audit indexing failed. StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
                    (int)response.StatusCode,
                    record.CorrelationId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Elasticsearch audit indexing failed. CorrelationId: {CorrelationId}",
                record.CorrelationId);
        }
    }
}

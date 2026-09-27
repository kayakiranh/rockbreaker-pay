using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;

namespace RockBreaker.Pay.Infrastructure.Auditing;

/// <summary>
/// TR: Audit kaydını önce MSSQL'e kalıcı olarak yazar, ardından Elasticsearch projection'ını oluşturur.
/// EN: Persists the audit record to MSSQL first, then creates its Elasticsearch projection.
/// Architecture: Durable System of Record + Search Projection.
/// </summary>
public sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IElasticAuditWriter _elasticAuditWriter;

    /// <summary>
    /// TR: Audit writer bağımlılıklarını alır.
    /// EN: Receives audit writer dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    /// <param name="elasticAuditWriter">TR: Elasticsearch adapter. EN: Elasticsearch adapter.</param>
    public AuditLogWriter(
        IDbConnectionFactory connectionFactory,
        IElasticAuditWriter elasticAuditWriter)
    {
        _connectionFactory = connectionFactory;
        _elasticAuditWriter = elasticAuditWriter;
    }

    /// <inheritdoc />
    public async Task WriteAsync(AuditLogRecord record)
    {
        const string sql = """
            INSERT INTO dbo.AuditLogs
                (CorrelationId, TraceId, HttpMethod, Path, RequestBody, ResponseBody,
                 ResponseStatusCode, ResponseTimeMs, IsSuccess, ClientIp, UserAgent, CreatedAtUtc)
            VALUES
                (@CorrelationId, @TraceId, @HttpMethod, @Path, @RequestBody, @ResponseBody,
                 @ResponseStatusCode, @ResponseTimeMs, @IsSuccess, @ClientIp, @UserAgent, @CreatedAtUtc);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, record);
        await _elasticAuditWriter.WriteAsync(record);
    }
}

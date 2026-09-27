namespace RockBreaker.Pay.Infrastructure.Auditing;

/// <summary>
/// TR: Audit kayıtlarının Elasticsearch'e indekslenmesini soyutlar.
/// EN: Abstracts indexing audit records into Elasticsearch.
/// Architecture: External Search Adapter.
/// </summary>
public interface IElasticAuditWriter
{
    /// <summary>
    /// TR: Audit kaydını Elasticsearch'e best-effort olarak gönderir.
    /// EN: Sends an audit record to Elasticsearch on a best-effort basis.
    /// Architecture: Secondary Audit Projection.
    /// </summary>
    /// <param name="record">TR: Audit kaydı. EN: Audit record.</param>
    Task WriteAsync(AuditLogRecord record);
}

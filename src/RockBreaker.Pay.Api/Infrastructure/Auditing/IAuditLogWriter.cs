namespace RockBreaker.Pay.Infrastructure.Auditing;

/// <summary>
/// TR: Audit kaydının kalıcı ve aranabilir hedeflere yazılmasını soyutlar.
/// EN: Abstracts writing audit records to durable and searchable destinations.
/// Architecture: Ports and Adapters + Dependency Inversion.
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>
    /// TR: Audit kaydını kalıcı audit store'a ve ikincil arama sistemine yazar.
    /// EN: Writes the audit record to the durable audit store and secondary search system.
    /// Architecture: Dual-write with MSSQL as system of record and Elasticsearch as searchable copy.
    /// </summary>
    /// <param name="record">TR: Audit kaydı. EN: Audit record.</param>
    Task WriteAsync(AuditLogRecord record);
}

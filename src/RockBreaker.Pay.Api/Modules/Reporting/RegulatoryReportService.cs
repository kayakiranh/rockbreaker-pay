using System.Globalization;
using System.Text;
using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;

namespace RockBreaker.Pay.Modules.Reporting;

/// <summary>
/// TR: MSSQL'deki kalıcı finansal/audit kayıtlarından regülasyon raporları üretir.
/// EN: Produces regulatory reports from durable financial/audit records in MSSQL.
/// Architecture: CQRS-style Read Service + Explicit CSV Exporter.
/// </summary>
public sealed class RegulatoryReportService : IRegulatoryReportService
{
    private const int MaximumRows = 100_000;
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// TR: Rapor servisi bağımlılıklarını alır.
    /// EN: Receives report-service dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    public RegulatoryReportService(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<TransactionReportRow>> GetTransactionsAsync(ReportQuery query)
    {
        ValidateQuery(query);

        const string sql = """
            SELECT TOP (@MaximumRows)
                Id AS TransactionId, SourceWalletId, DestinationWalletId, Amount, Currency,
                Type, Status, IdempotencyKey, CorrelationId, CreatedAtUtc, CompletedAtUtc
            FROM dbo.WalletTransactions
            WHERE CreatedAtUtc >= @FromUtc
              AND CreatedAtUtc < @ToUtc
              AND (@WalletId IS NULL OR SourceWalletId = @WalletId OR DestinationWalletId = @WalletId)
            ORDER BY CreatedAtUtc, Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return (await connection.QueryAsync<TransactionReportRow>(
            sql,
            new { MaximumRows, query.FromUtc, query.ToUtc, query.WalletId })).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FraudReportRow>> GetFraudEventsAsync(ReportQuery query)
    {
        ValidateQuery(query);

        const string sql = """
            SELECT TOP (@MaximumRows)
                Id, WalletId, Amount, RiskScore, Action, TriggeredRulesJson, CreatedAtUtc
            FROM dbo.FraudEvents
            WHERE CreatedAtUtc >= @FromUtc
              AND CreatedAtUtc < @ToUtc
              AND (@WalletId IS NULL OR WalletId = @WalletId)
            ORDER BY CreatedAtUtc, Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return (await connection.QueryAsync<FraudReportRow>(
            sql,
            new { MaximumRows, query.FromUtc, query.ToUtc, query.WalletId })).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<AuditReportRow>> GetAuditLogsAsync(ReportQuery query)
    {
        ValidateQuery(query);

        const string sql = """
            SELECT TOP (@MaximumRows)
                Id, CorrelationId, TraceId, HttpMethod, Path, RequestBody, ResponseBody,
                ResponseStatusCode, ResponseTimeMs, IsSuccess, ClientIp, UserAgent, CreatedAtUtc
            FROM dbo.AuditLogs
            WHERE CreatedAtUtc >= @FromUtc
              AND CreatedAtUtc < @ToUtc
            ORDER BY CreatedAtUtc, Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return (await connection.QueryAsync<AuditReportRow>(
            sql,
            new { MaximumRows, query.FromUtc, query.ToUtc })).ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportTransactionsCsvAsync(ReportQuery query)
    {
        var rows = await GetTransactionsAsync(query);
        var builder = CreateCsvBuilder(
            "TransactionId,SourceWalletId,DestinationWalletId,Amount,Currency,Type,Status,IdempotencyKey,CorrelationId,CreatedAtUtc,CompletedAtUtc");

        foreach (var row in rows)
        {
            AppendCsvRow(builder,
                row.TransactionId,
                row.SourceWalletId,
                row.DestinationWalletId,
                row.Amount.ToString(CultureInfo.InvariantCulture),
                row.Currency,
                row.Type,
                row.Status,
                row.IdempotencyKey,
                row.CorrelationId,
                row.CreatedAtUtc.ToString("O"),
                row.CompletedAtUtc?.ToString("O"));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportFraudCsvAsync(ReportQuery query)
    {
        var rows = await GetFraudEventsAsync(query);
        var builder = CreateCsvBuilder("Id,WalletId,Amount,RiskScore,Action,TriggeredRulesJson,CreatedAtUtc");

        foreach (var row in rows)
        {
            AppendCsvRow(builder,
                row.Id,
                row.WalletId,
                row.Amount.ToString(CultureInfo.InvariantCulture),
                row.RiskScore,
                row.Action,
                row.TriggeredRulesJson,
                row.CreatedAtUtc.ToString("O"));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportAuditCsvAsync(ReportQuery query)
    {
        var rows = await GetAuditLogsAsync(query);
        var builder = CreateCsvBuilder(
            "Id,CorrelationId,TraceId,HttpMethod,Path,RequestBody,ResponseBody,ResponseStatusCode,ResponseTimeMs,IsSuccess,ClientIp,UserAgent,CreatedAtUtc");

        foreach (var row in rows)
        {
            AppendCsvRow(builder,
                row.Id,
                row.CorrelationId,
                row.TraceId,
                row.HttpMethod,
                row.Path,
                row.RequestBody,
                row.ResponseBody,
                row.ResponseStatusCode,
                row.ResponseTimeMs,
                row.IsSuccess,
                row.ClientIp,
                row.UserAgent,
                row.CreatedAtUtc.ToString("O"));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    /// <summary>
    /// TR: Rapor tarih aralığının geçerli ve kontrollü boyutta olduğunu doğrular.
    /// EN: Validates that the report date range is valid and bounded.
    /// Architecture: Guard Clause.
    /// </summary>
    /// <param name="query">TR: Rapor sorgusu. EN: Report query.</param>
    private static void ValidateQuery(ReportQuery query)
    {
        if (query.FromUtc >= query.ToUtc)
        {
            throw new ArgumentException("FromUtc must be earlier than ToUtc.");
        }

        if (query.ToUtc - query.FromUtc > TimeSpan.FromDays(366))
        {
            throw new ArgumentException("A single report request cannot exceed 366 days.");
        }
    }

    /// <summary>
    /// TR: CSV başlığıyla yeni builder oluşturur.
    /// EN: Creates a new builder with the CSV header.
    /// Architecture: Explicit Export Helper.
    /// </summary>
    /// <param name="header">TR: CSV başlığı. EN: CSV header.</param>
    /// <returns>TR: StringBuilder. EN: StringBuilder.</returns>
    private static StringBuilder CreateCsvBuilder(string header) =>
        new StringBuilder().AppendLine(header);

    /// <summary>
    /// TR: Değerleri RFC4180 uyumlu biçimde escape ederek CSV satırı ekler.
    /// EN: Adds a CSV row while escaping values in an RFC4180-compatible manner.
    /// Architecture: Explicit Export Helper.
    /// </summary>
    /// <param name="builder">TR: CSV builder. EN: CSV builder.</param>
    /// <param name="values">TR: Satır değerleri. EN: Row values.</param>
    private static void AppendCsvRow(StringBuilder builder, params object?[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    /// <summary>
    /// TR: CSV alanındaki tırnak ve satır sonlarını güvenli biçimde escape eder.
    /// EN: Safely escapes quotes and line breaks in a CSV field.
    /// Architecture: Export Sanitization.
    /// </summary>
    /// <param name="value">TR: Alan değeri. EN: Field value.</param>
    /// <returns>TR: Escape edilmiş CSV değeri. EN: Escaped CSV value.</returns>
    private static string EscapeCsv(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
}

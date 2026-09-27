using System.Diagnostics;
using System.Text;

namespace RockBreaker.Pay.Infrastructure.Auditing;

/// <summary>
/// TR: HTTP isteklerini denetlenebilir kayıtlara dönüştürür; health/OpenAPI endpoint'lerini audit bağımlılığından ayırır.
/// EN: Converts HTTP requests into auditable records while keeping health/OpenAPI endpoints independent from audit storage.
/// Architecture: ASP.NET Core Middleware + Cross-Cutting Concern + Failure Isolation.
/// </summary>
public sealed class RequestAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestAuditMiddleware> _logger;

    /// <summary>
    /// TR: Middleware zincirindeki sonraki bileşeni ve logger'ı alır.
    /// EN: Receives the next middleware component and logger.
    /// Architecture: Chain of Responsibility + Constructor Injection.
    /// </summary>
    /// <param name="next">TR: Sonraki middleware. EN: Next middleware.</param>
    /// <param name="logger">TR: Uygulama logger'ı. EN: Application logger.</param>
    public RequestAuditMiddleware(
        RequestDelegate next,
        ILogger<RequestAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// TR: İstek/response body'lerini kopyalar, maskeler ve audit store'a best-effort yazar.
    /// EN: Copies and masks request/response bodies and writes them to the audit store on a best-effort basis.
    /// Architecture: Middleware + Observability + Failure Isolation.
    /// </summary>
    /// <param name="context">TR: HTTP context. EN: HTTP context.</param>
    /// <param name="auditLogWriter">TR: Audit writer. EN: Audit writer.</param>
    public async Task InvokeAsync(HttpContext context, IAuditLogWriter auditLogWriter)
    {
        if (ShouldBypassAudit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);

        var originalResponseBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            responseBuffer.Position = 0;
            var responseBody = await new StreamReader(
                    responseBuffer,
                    Encoding.UTF8,
                    leaveOpen: true)
                .ReadToEndAsync();

            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            var record = new AuditLogRecord
            {
                CorrelationId = context.TraceIdentifier,
                TraceId = Activity.Current?.TraceId.ToString(),
                HttpMethod = context.Request.Method,
                Path = context.Request.Path,
                RequestBody = SensitiveDataMasker.Mask(requestBody),
                ResponseBody = SensitiveDataMasker.Mask(responseBody),
                ResponseStatusCode = context.Response.StatusCode,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                IsSuccess = context.Response.StatusCode < 400,
                ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                await auditLogWriter.WriteAsync(record);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Audit persistence failed without changing the HTTP response. CorrelationId: {CorrelationId}, Path: {Path}",
                    record.CorrelationId,
                    record.Path);
            }
        }
    }

    /// <summary>
    /// TR: Liveness/readiness ve OpenAPI endpoint'lerini audit persistence bağımlılığından ayırır.
    /// EN: Keeps liveness/readiness and OpenAPI endpoints independent from audit persistence.
    /// Architecture: Operational Endpoint Isolation.
    /// </summary>
    /// <param name="path">TR: Request path. EN: Request path.</param>
    /// <returns>TR: Audit atlanmalıysa true. EN: True when audit should be skipped.</returns>
    private static bool ShouldBypassAudit(PathString path) =>
        path.StartsWithSegments("/health")
        || path.StartsWithSegments("/openapi");

    /// <summary>
    /// TR: Request body'yi downstream pipeline bozulmadan okur.
    /// EN: Reads the request body without breaking the downstream pipeline.
    /// Architecture: Request Buffering Helper.
    /// </summary>
    /// <param name="request">TR: HTTP request. EN: HTTP request.</param>
    /// <returns>TR: Request body veya null. EN: Request body or null.</returns>
    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
        {
            return null;
        }

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        return body;
    }
}

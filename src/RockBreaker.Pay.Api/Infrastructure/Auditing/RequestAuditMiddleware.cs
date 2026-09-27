using System.Diagnostics;
using System.Text;

namespace RockBreaker.Pay.Infrastructure.Auditing;

/// <summary>
/// TR: Her HTTP isteğinde request/response içeriği, süre, başarı durumu ve istemci bilgilerini audit kaydına dönüştürür.
/// EN: Converts request/response content, duration, outcome and client metadata into an audit record for every HTTP request.
/// Architecture: ASP.NET Core Middleware + Cross-Cutting Concern.
/// </summary>
public sealed class RequestAuditMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// TR: Middleware zincirindeki sonraki bileşeni alır.
    /// EN: Receives the next component in the middleware pipeline.
    /// Architecture: Chain of Responsibility.
    /// </summary>
    /// <param name="next">TR: Sonraki middleware. EN: Next middleware.</param>
    public RequestAuditMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// TR: İsteği geçirirken body'leri güvenli biçimde kopyalar ve response tamamlandıktan sonra audit kaydı oluşturur.
    /// EN: Safely copies bodies while passing the request through and creates an audit record after the response completes.
    /// Architecture: Middleware + Observability.
    /// </summary>
    /// <param name="context">TR: HTTP context. EN: HTTP context.</param>
    /// <param name="auditLogWriter">TR: Audit writer. EN: Audit writer.</param>
    public async Task InvokeAsync(HttpContext context, IAuditLogWriter auditLogWriter)
    {
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
            var responseBody = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
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

            await auditLogWriter.WriteAsync(record);
        }
    }

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

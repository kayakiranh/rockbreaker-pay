using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.FakeNotification.Controllers;

/// <summary>
/// TR: SMS, e-mail ve push notification gönderimini simüle eder.
/// EN: Simulates SMS, e-mail and push notification delivery.
/// Architecture: Fake External Service.
/// </summary>
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    /// <summary>
    /// TR: Fake SMS gönderir.
    /// EN: Sends a fake SMS.
    /// Architecture: REST Command.
    /// </summary>
    /// <param name="request">TR: Bildirim isteği. EN: Notification request.</param>
    /// <returns>TR: Gönderim sonucu. EN: Delivery result.</returns>
    [HttpPost("sms")]
    public ActionResult<NotificationResult> SendSms([FromBody] NotificationRequest request) => Ok(CreateResult("SMS", request));

    /// <summary>
    /// TR: Fake e-mail gönderir.
    /// EN: Sends a fake e-mail.
    /// Architecture: REST Command.
    /// </summary>
    /// <param name="request">TR: Bildirim isteği. EN: Notification request.</param>
    /// <returns>TR: Gönderim sonucu. EN: Delivery result.</returns>
    [HttpPost("email")]
    public ActionResult<NotificationResult> SendEmail([FromBody] NotificationRequest request) => Ok(CreateResult("EMAIL", request));

    /// <summary>
    /// TR: Fake push notification gönderir.
    /// EN: Sends a fake push notification.
    /// Architecture: REST Command.
    /// </summary>
    /// <param name="request">TR: Bildirim isteği. EN: Notification request.</param>
    /// <returns>TR: Gönderim sonucu. EN: Delivery result.</returns>
    [HttpPost("push")]
    public ActionResult<NotificationResult> SendPush([FromBody] NotificationRequest request) => Ok(CreateResult("PUSH", request));

    private static NotificationResult CreateResult(string channel, NotificationRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Channel = channel,
        Recipient = request.Recipient,
        Status = "Sent",
        CreatedAtUtc = DateTime.UtcNow
    };
}

/// <summary>
/// TR: Fake notification isteğidir.
/// EN: Fake notification request.
/// Architecture: Request DTO.
/// </summary>
public sealed class NotificationRequest
{
    /// <summary>TR: Alıcı bilgisi. EN: Recipient value. Architecture: DTO Property.</summary>
    public string Recipient { get; init; } = string.Empty;
    /// <summary>TR: Mesaj içeriği. EN: Message content. Architecture: DTO Property.</summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// TR: Fake notification gönderim sonucudur.
/// EN: Fake notification delivery result.
/// Architecture: Response DTO.
/// </summary>
public sealed class NotificationResult
{
    /// <summary>TR: Gönderim kimliği. EN: Delivery identifier. Architecture: DTO Property.</summary>
    public Guid Id { get; init; }
    /// <summary>TR: Kanal. EN: Channel. Architecture: DTO Property.</summary>
    public string Channel { get; init; } = string.Empty;
    /// <summary>TR: Alıcı. EN: Recipient. Architecture: DTO Property.</summary>
    public string Recipient { get; init; } = string.Empty;
    /// <summary>TR: Gönderim durumu. EN: Delivery status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>TR: UTC oluşturulma zamanı. EN: UTC creation time. Architecture: Audit Metadata.</summary>
    public DateTime CreatedAtUtc { get; init; }
}

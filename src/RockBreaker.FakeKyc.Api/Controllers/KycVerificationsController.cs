using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.FakeKyc.Controllers;

/// <summary>
/// TR: Harici bir KYC sağlayıcısının kimlik doğrulama davranışını deterministik kurallarla simüle eder.
/// EN: Simulates an external KYC provider's identity-verification behavior using deterministic rules.
/// Architecture: Fake External Service + In-Memory Store.
/// </summary>
[ApiController]
[Route("api/kyc/verifications")]
public sealed class KycVerificationsController : ControllerBase
{
    private static readonly ConcurrentDictionary<Guid, KycVerificationResponse> Verifications = new();

    /// <summary>
    /// TR: Kullanıcı bilgilerini değerlendirip Verified, Pending veya Rejected kararı üretir.
    /// EN: Evaluates user information and produces a Verified, Pending or Rejected decision.
    /// Architecture: Fake Rule Engine + REST Command.
    /// </summary>
    /// <param name="request">TR: KYC doğrulama isteği. EN: KYC verification request.</param>
    /// <returns>TR: Fake provider doğrulama sonucu. EN: Fake provider verification result.</returns>
    [HttpPost]
    public ActionResult<KycVerificationResponse> Verify([FromBody] KycVerificationRequest request)
    {
        var result = Evaluate(request);
        Verifications[result.VerificationId] = result;
        return Ok(result);
    }

    /// <summary>
    /// TR: Daha önce üretilmiş fake KYC doğrulama sonucunu kimliğine göre döndürür.
    /// EN: Returns a previously created fake KYC verification result by identifier.
    /// Architecture: REST Query + In-Memory Repository.
    /// </summary>
    /// <param name="verificationId">TR: Provider doğrulama kimliği. EN: Provider verification identifier.</param>
    /// <returns>TR: Doğrulama sonucu veya 404. EN: Verification result or 404.</returns>
    [HttpGet("{verificationId:guid}")]
    public ActionResult<KycVerificationResponse> Get(Guid verificationId) =>
        Verifications.TryGetValue(verificationId, out var result)
            ? Ok(result)
            : NotFound();

    /// <summary>
    /// TR: Fake provider senaryosunu açık ve tekrar üretilebilir kurallarla değerlendirir.
    /// EN: Evaluates the fake-provider scenario with explicit and reproducible rules.
    /// Architecture: Deterministic Fake Rule Engine.
    /// </summary>
    /// <param name="request">TR: KYC isteği. EN: KYC request.</param>
    /// <returns>TR: Fake provider kararı. EN: Fake provider decision.</returns>
    private static KycVerificationResponse Evaluate(KycVerificationRequest request)
    {
        var verificationId = Guid.NewGuid();

        if (request.UserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return Create(
                verificationId,
                request.UserId,
                "Rejected",
                100,
                "Required identity fields are missing.");
        }

        if (!request.Email.Contains('@', StringComparison.Ordinal))
        {
            return Create(
                verificationId,
                request.UserId,
                "Rejected",
                95,
                "E-mail format is invalid.");
        }

        var domain = request.Email.Split('@', 2)[1];

        if (domain.Equals("reject.local", StringComparison.OrdinalIgnoreCase))
        {
            return Create(
                verificationId,
                request.UserId,
                "Rejected",
                90,
                "Fake provider rejection scenario was triggered.");
        }

        if (domain.Equals("manual.local", StringComparison.OrdinalIgnoreCase))
        {
            return Create(
                verificationId,
                request.UserId,
                "Pending",
                55,
                "Manual review is required by the fake provider.");
        }

        return Create(
            verificationId,
            request.UserId,
            "Verified",
            10,
            "Identity data passed fake KYC checks.");
    }

    /// <summary>
    /// TR: Standart fake KYC response nesnesini oluşturur.
    /// EN: Creates the standard fake KYC response object.
    /// Architecture: Explicit Response Factory.
    /// </summary>
    private static KycVerificationResponse Create(
        Guid verificationId,
        Guid userId,
        string status,
        int riskScore,
        string reason) =>
        new()
        {
            VerificationId = verificationId,
            UserId = userId,
            Status = status,
            RiskScore = riskScore,
            Reason = reason,
            VerifiedAtUtc = status == "Verified" ? DateTime.UtcNow : null,
            CreatedAtUtc = DateTime.UtcNow
        };
}

/// <summary>
/// TR: Fake KYC sağlayıcısına gönderilen doğrulama isteğidir.
/// EN: Verification request sent to the fake KYC provider.
/// Architecture: Request DTO.
/// </summary>
public sealed class KycVerificationRequest
{
    /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: DTO Property.</summary>
    public Guid UserId { get; init; }

    /// <summary>TR: Kullanıcı adı. EN: User first name. Architecture: DTO Property.</summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>TR: Kullanıcı soyadı. EN: User last name. Architecture: DTO Property.</summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>TR: Kullanıcı e-postası. EN: User e-mail. Architecture: DTO Property.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>TR: Opsiyonel telefon numarası. EN: Optional phone number. Architecture: DTO Property.</summary>
    public string? Phone { get; init; }
}

/// <summary>
/// TR: Fake KYC sağlayıcısının doğrulama sonucudur.
/// EN: Verification result returned by the fake KYC provider.
/// Architecture: Response DTO.
/// </summary>
public sealed class KycVerificationResponse
{
    /// <summary>TR: Provider doğrulama kimliği. EN: Provider verification identifier. Architecture: DTO Property.</summary>
    public Guid VerificationId { get; init; }

    /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: DTO Property.</summary>
    public Guid UserId { get; init; }

    /// <summary>TR: Verified, Pending veya Rejected kararı. EN: Verified, Pending or Rejected decision. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>TR: Fake risk skoru. EN: Fake risk score. Architecture: Explainability Property.</summary>
    public int RiskScore { get; init; }

    /// <summary>TR: Karar gerekçesi. EN: Decision reason. Architecture: Explainability Property.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>TR: Verified ise doğrulama UTC zamanı. EN: UTC verification time when Verified. Architecture: Audit Metadata.</summary>
    public DateTime? VerifiedAtUtc { get; init; }

    /// <summary>TR: Provider kaydının UTC oluşturulma zamanı. EN: UTC provider-record creation time. Architecture: Audit Metadata.</summary>
    public DateTime CreatedAtUtc { get; init; }
}

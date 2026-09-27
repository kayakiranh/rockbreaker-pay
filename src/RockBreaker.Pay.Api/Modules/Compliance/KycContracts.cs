namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: KYC durumunu API'ye taşır.
/// EN: Carries KYC status to the API.
/// Architecture: Response DTO.
/// </summary>
public sealed class KycStatusResponse
{
    /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: DTO Property.</summary>
    public Guid UserId { get; init; }

    /// <summary>TR: Güncel KYC durumu. EN: Current KYC status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// TR: Admin KYC inceleme kararı isteğidir.
/// EN: Admin request for a KYC review decision.
/// Architecture: Request DTO.
/// </summary>
public sealed class ReviewKycRequest
{
    /// <summary>TR: Verified veya Rejected hedef durumu. EN: Target status: Verified or Rejected. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>TR: İnceleme açıklaması. EN: Review reason. Architecture: DTO Property.</summary>
    public string? Reason { get; init; }
}

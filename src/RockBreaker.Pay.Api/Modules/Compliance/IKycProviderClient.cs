using RockBreaker.Pay.Modules.Identity.Domain;

namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: Harici KYC sağlayıcısına erişimi ana uygulamadan soyutlar.
/// EN: Abstracts access to the external KYC provider from the main application.
/// Architecture: Anti-Corruption Layer + Dependency Inversion.
/// </summary>
public interface IKycProviderClient
{
    /// <summary>
    /// TR: Kullanıcı verisini harici KYC sağlayıcısında doğrular.
    /// EN: Verifies user data through the external KYC provider.
    /// Architecture: External Command Port.
    /// </summary>
    /// <param name="user">TR: Doğrulanacak kullanıcı. EN: User to verify.</param>
    /// <returns>TR: Provider kararı. EN: Provider decision.</returns>
    Task<KycProviderResult> VerifyAsync(UserAccount user);
}

/// <summary>
/// TR: Harici KYC sağlayıcısının normalize edilmiş kararını temsil eder.
/// EN: Represents the normalized decision returned by the external KYC provider.
/// Architecture: Anti-Corruption Result Model.
/// </summary>
public sealed class KycProviderResult
{
    /// <summary>TR: Provider doğrulama kimliği. EN: Provider verification identifier. Architecture: DTO Property.</summary>
    public Guid VerificationId { get; init; }

    /// <summary>TR: Normalize KYC durumu. EN: Normalized KYC status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>TR: Provider risk skoru. EN: Provider risk score. Architecture: Explainability Property.</summary>
    public int RiskScore { get; init; }

    /// <summary>TR: Provider karar gerekçesi. EN: Provider decision reason. Architecture: Explainability Property.</summary>
    public string Reason { get; init; } = string.Empty;
}

namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: KYC yaşam döngüsünde kullanılan izinli durum değerlerini merkezi olarak tanımlar.
/// EN: Centrally defines allowed KYC lifecycle status values.
/// Architecture: Explicit Compliance State Vocabulary.
/// </summary>
public static class KycStates
{
    /// <summary>TR: Kullanıcı KYC sürecini başlatmadı. EN: User has not started KYC. Architecture: Compliance State.</summary>
    public const string NotStarted = "NotStarted";

    /// <summary>TR: KYC inceleme bekliyor. EN: KYC is awaiting review. Architecture: Compliance State.</summary>
    public const string Pending = "Pending";

    /// <summary>TR: KYC doğrulandı. EN: KYC is verified. Architecture: Compliance State.</summary>
    public const string Verified = "Verified";

    /// <summary>TR: KYC reddedildi. EN: KYC was rejected. Architecture: Compliance State.</summary>
    public const string Rejected = "Rejected";

    /// <summary>
    /// TR: Değerin izinli bir KYC durumu olup olmadığını kontrol eder.
    /// EN: Checks whether a value is an allowed KYC status.
    /// Architecture: Domain Validation Helper.
    /// </summary>
    /// <param name="value">TR: KYC durumu. EN: KYC status.</param>
    /// <returns>TR: Geçerliyse true. EN: True when valid.</returns>
    public static bool IsValid(string value) =>
        value is NotStarted or Pending or Verified or Rejected;
}

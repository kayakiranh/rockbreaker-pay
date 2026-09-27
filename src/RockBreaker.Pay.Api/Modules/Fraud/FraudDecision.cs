namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Fraud değerlendirme sonucunu taşır.
/// EN: Carries the result of a fraud evaluation.
/// Architecture: Rule Engine Decision DTO.
/// </summary>
public sealed class FraudDecision
{
    /// <summary>TR: Uygulanacak fraud aksiyonu. EN: Fraud action to apply. Architecture: Decision Property.</summary>
    public FraudAction Action { get; init; }

    /// <summary>TR: Tetiklenen fraud kural kodları. EN: Triggered fraud rule codes. Architecture: Explainable Risk Decision.</summary>
    public IReadOnlyCollection<string> TriggeredRules { get; init; } = Array.Empty<string>();

    /// <summary>TR: Hesaplanan risk skoru. EN: Calculated risk score. Architecture: Explainable Risk Decision.</summary>
    public int RiskScore { get; init; }
}

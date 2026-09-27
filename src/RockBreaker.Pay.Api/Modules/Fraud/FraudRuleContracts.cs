namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Yönetim API'sinde görüntülenen fraud kuralını temsil eder.
/// EN: Represents a fraud rule displayed through the management API.
/// Architecture: Response DTO.
/// </summary>
public sealed class FraudRuleResponse
{
    /// <summary>TR: Fraud kuralının benzersiz kimliği. EN: Unique fraud-rule identifier. Architecture: DTO Property.</summary>
    public int Id { get; init; }

    /// <summary>TR: İnsan ve sistem tarafından okunabilir kural kodu. EN: Human- and machine-readable rule code. Architecture: DTO Property.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>TR: Kural değerlendirme tipi. EN: Rule evaluation type. Architecture: DTO Property.</summary>
    public string RuleType { get; init; } = string.Empty;

    /// <summary>TR: Kural eşik değeri. EN: Rule threshold value. Architecture: DTO Property.</summary>
    public decimal ThresholdValue { get; init; }

    /// <summary>TR: Kural tetiklendiğinde eklenecek risk puanı. EN: Risk score added when the rule is triggered. Architecture: DTO Property.</summary>
    public int RiskScore { get; init; }

    /// <summary>TR: Kuralın önerdiği fraud aksiyonu. EN: Fraud action recommended by the rule. Architecture: DTO Property.</summary>
    public FraudAction Action { get; init; }

    /// <summary>TR: Kuralın aktif olup olmadığı. EN: Whether the rule is active. Architecture: DTO Property.</summary>
    public bool IsEnabled { get; init; }
}

/// <summary>
/// TR: Fraud kuralının operasyon sırasında değiştirilebilir alanlarını güncelleme isteğidir.
/// EN: Request for updating operationally mutable fields of a fraud rule.
/// Architecture: Command DTO.
/// </summary>
public sealed class UpdateFraudRuleRequest
{
    /// <summary>TR: Yeni eşik değeri. EN: New threshold value. Architecture: DTO Property.</summary>
    public decimal ThresholdValue { get; init; }

    /// <summary>TR: Yeni risk skoru. EN: New risk score. Architecture: DTO Property.</summary>
    public int RiskScore { get; init; }

    /// <summary>TR: Yeni fraud aksiyonu. EN: New fraud action. Architecture: DTO Property.</summary>
    public FraudAction Action { get; init; }

    /// <summary>TR: Kuralın aktiflik durumu. EN: Rule enabled state. Architecture: DTO Property.</summary>
    public bool IsEnabled { get; init; }
}

using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Fraud kurallarının görüntülenmesi ve kontrollü güncellenmesi için application service sözleşmesidir.
/// EN: Application-service contract for viewing and controlled updating of fraud rules.
/// Architecture: Application Service + Dependency Inversion.
/// </summary>
public interface IFraudRuleService
{
    /// <summary>
    /// TR: Tüm fraud kurallarını döndürür.
    /// EN: Returns all fraud rules.
    /// Architecture: Application Query.
    /// </summary>
    /// <returns>TR: Fraud kuralları. EN: Fraud rules.</returns>
    Task<IReadOnlyCollection<FraudRuleResponse>> GetRulesAsync();

    /// <summary>
    /// TR: Fraud kuralının eşik, risk, aksiyon ve aktiflik değerlerini günceller.
    /// EN: Updates threshold, risk, action and enabled state of a fraud rule.
    /// Architecture: Application Command.
    /// </summary>
    /// <param name="ruleId">TR: Kural kimliği. EN: Rule identifier.</param>
    /// <param name="request">TR: Güncellenecek değerler. EN: Values to update.</param>
    /// <returns>TR: Güncel kural veya hata. EN: Updated rule or error.</returns>
    Task<OperationResult<FraudRuleResponse>> UpdateAsync(int ruleId, UpdateFraudRuleRequest request);
}

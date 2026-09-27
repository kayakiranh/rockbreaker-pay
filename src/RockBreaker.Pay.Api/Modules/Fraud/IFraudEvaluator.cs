namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Finansal işlem öncesi fraud kurallarını çalıştıran sözleşmedir.
/// EN: Contract for executing fraud rules before a financial transaction.
/// Architecture: Strategy Pattern + Dependency Inversion.
/// </summary>
public interface IFraudEvaluator
{
    /// <summary>
    /// TR: Kaynak wallet ve tutar için aktif fraud kurallarını değerlendirir.
    /// EN: Evaluates active fraud rules for the source wallet and amount.
    /// Architecture: Rule Engine Strategy.
    /// </summary>
    /// <param name="walletId">TR: Kaynak wallet kimliği. EN: Source wallet identifier.</param>
    /// <param name="amount">TR: İşlem tutarı. EN: Transaction amount.</param>
    /// <returns>TR: Fraud kararı. EN: Fraud decision.</returns>
    Task<FraudDecision> EvaluateAsync(Guid walletId, decimal amount);
}

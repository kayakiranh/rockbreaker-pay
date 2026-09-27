namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Fraud motorunun bir işleme verebileceği aksiyonu belirtir.
/// EN: Defines the action the fraud engine can assign to a transaction.
/// Architecture: Rule Engine Decision Model.
/// </summary>
public enum FraudAction
{
    /// <summary>TR: İşlem devam eder. EN: Transaction may continue. Architecture: Risk Decision.</summary>
    Allow = 0,
    /// <summary>TR: İşlem reddedilir fakat wallet açık kalır. EN: Transaction is rejected while wallet remains active. Architecture: Risk Decision.</summary>
    Reject = 1,
    /// <summary>TR: İşlem reddedilir ve wallet bloke edilir. EN: Transaction is rejected and wallet is blocked. Architecture: Risk Decision.</summary>
    BlockWallet = 2
}

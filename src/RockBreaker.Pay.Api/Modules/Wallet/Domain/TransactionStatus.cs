namespace RockBreaker.Pay.Modules.Wallet.Domain;

/// <summary>
/// TR: Finansal işlemin yaşam döngüsünü belirtir.
/// EN: Defines the lifecycle of a financial transaction.
/// Architecture: Explicit State Model.
/// </summary>
public enum TransactionStatus
{
    /// <summary>TR: İşlem bekliyor. EN: Transaction is pending. Architecture: State Model.</summary>
    Pending = 1,
    /// <summary>TR: İşlem yürütülüyor. EN: Transaction is processing. Architecture: State Model.</summary>
    Processing = 2,
    /// <summary>TR: İşlem tamamlandı. EN: Transaction completed. Architecture: State Model.</summary>
    Completed = 3,
    /// <summary>TR: İşlem başarısız oldu. EN: Transaction failed. Architecture: State Model.</summary>
    Failed = 4,
    /// <summary>TR: İşlem iptal edildi. EN: Transaction cancelled. Architecture: State Model.</summary>
    Cancelled = 5,
    /// <summary>TR: İşlem ters kayıtla geri alındı. EN: Transaction was reversed. Architecture: Compensating Transaction.</summary>
    Reversed = 6,
    /// <summary>TR: İşlem fraud incelemesinde. EN: Transaction is under fraud review. Architecture: Risk State.</summary>
    FraudReview = 7
}

namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Otomatik ödeme talimatının yaşam döngüsü durumunu belirtir.
/// EN: Defines the lifecycle status of an automatic payment instruction.
/// Architecture: Explicit State Model.
/// </summary>
public enum PaymentInstructionStatus
{
    /// <summary>TR: Talimat aktif. EN: Instruction is active. Architecture: State Model.</summary>
    Active = 1,
    /// <summary>TR: Talimat duraklatıldı. EN: Instruction is paused. Architecture: State Model.</summary>
    Paused = 2,
    /// <summary>TR: Talimat iptal edildi. EN: Instruction is cancelled. Architecture: State Model.</summary>
    Cancelled = 3,
    /// <summary>TR: Tek seferlik talimat tamamlandı. EN: One-time instruction completed. Architecture: State Model.</summary>
    Completed = 4,
    /// <summary>TR: Son çalıştırma başarısız oldu. EN: Last execution failed. Architecture: State Model.</summary>
    Failed = 5
}

/// <summary>
/// TR: Otomatik talimat tekrar sıklığını belirtir.
/// EN: Defines automatic instruction recurrence.
/// Architecture: Scheduling Value.
/// </summary>
public enum PaymentInstructionFrequency
{
    /// <summary>TR: Tek seferlik. EN: One time. Architecture: Schedule Type.</summary>
    Once = 1,
    /// <summary>TR: Günlük. EN: Daily. Architecture: Schedule Type.</summary>
    Daily = 2,
    /// <summary>TR: Haftalık. EN: Weekly. Architecture: Schedule Type.</summary>
    Weekly = 3,
    /// <summary>TR: Aylık. EN: Monthly. Architecture: Schedule Type.</summary>
    Monthly = 4
}

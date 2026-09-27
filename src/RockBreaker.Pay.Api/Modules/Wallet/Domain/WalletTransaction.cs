namespace RockBreaker.Pay.Modules.Wallet.Domain;

/// <summary>
/// TR: Finansal para hareketinin ana işlem kaydıdır.
/// EN: Master business record of a financial money movement.
/// Architecture: Transaction Header + Ledger Model.
/// </summary>
public sealed class WalletTransaction
{
    /// <summary>TR: İşlem kimliği. EN: Transaction identifier. Architecture: Entity Identity.</summary>
    public Guid Id { get; set; }
    /// <summary>TR: Kaynak wallet. EN: Source wallet. Architecture: Aggregate Reference.</summary>
    public Guid? SourceWalletId { get; set; }
    /// <summary>TR: Hedef wallet. EN: Destination wallet. Architecture: Aggregate Reference.</summary>
    public Guid? DestinationWalletId { get; set; }
    /// <summary>TR: İşlem tutarı. EN: Transaction amount. Architecture: Monetary Value.</summary>
    public decimal Amount { get; set; }
    /// <summary>TR: Para birimi. EN: Currency. Architecture: Monetary Metadata.</summary>
    public string Currency { get; set; } = "TRY";
    /// <summary>TR: İşlem türü. EN: Transaction type. Architecture: Business Classification.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>TR: İşlem durumu. EN: Transaction status. Architecture: Explicit State Model.</summary>
    public TransactionStatus Status { get; set; }
    /// <summary>TR: Tekrarlı finansal etkiyi engelleyen idempotency anahtarı. EN: Idempotency key preventing duplicate financial effects. Architecture: Idempotency Pattern.</summary>
    public string? IdempotencyKey { get; set; }
    /// <summary>TR: Uçtan uca takip kimliği. EN: End-to-end correlation identifier. Architecture: Observability.</summary>
    public string CorrelationId { get; set; } = string.Empty;
    /// <summary>TR: UTC oluşturulma zamanı. EN: UTC creation time. Architecture: Audit Metadata.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>TR: UTC tamamlanma zamanı. EN: UTC completion time. Architecture: Audit Metadata.</summary>
    public DateTime? CompletedAtUtc { get; set; }
}

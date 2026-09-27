namespace RockBreaker.Pay.Modules.Wallet.Domain;

/// <summary>
/// TR: Her finansal işlem için oluşan debit/credit ledger kaydıdır.
/// EN: Debit/credit ledger entry created for each financial transaction.
/// Architecture: Double-Entry Bookkeeping; hareketlerin denetlenebilir ve yeniden hesaplanabilir olmasını sağlar.
/// </summary>
public sealed class LedgerEntry
{
    /// <summary>TR: Ledger kimliği. EN: Ledger entry identifier. Architecture: Entity Identity.</summary>
    public Guid Id { get; set; }
    /// <summary>TR: Finansal işlem kimliği. EN: Financial transaction identifier. Architecture: Transaction Reference.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>TR: İlgili wallet kimliği. EN: Related wallet identifier. Architecture: Aggregate Reference.</summary>
    public Guid WalletId { get; set; }
    /// <summary>TR: Debit/Credit yönü. EN: Debit/Credit direction. Architecture: Double-Entry Bookkeeping.</summary>
    public LedgerDirection Direction { get; set; }
    /// <summary>TR: Ledger tutarı. EN: Ledger amount. Architecture: Monetary Value.</summary>
    public decimal Amount { get; set; }
    /// <summary>TR: Hareket sonrası bakiye. EN: Balance after entry. Architecture: Audit Snapshot.</summary>
    public decimal BalanceAfter { get; set; }
    /// <summary>TR: Para birimi. EN: Currency. Architecture: Monetary Metadata.</summary>
    public string Currency { get; set; } = "TRY";
    /// <summary>TR: UTC kayıt zamanı. EN: UTC creation time. Architecture: Immutable Audit Timeline.</summary>
    public DateTime CreatedAtUtc { get; set; }
}

namespace RockBreaker.Pay.Modules.Wallet.Domain;

/// <summary>
/// TR: Kullanıcının dijital cüzdanını temsil eden aggregate root'tur.
/// EN: Aggregate root representing a user's digital wallet.
/// Architecture: Domain Model + Aggregate Root.
/// </summary>
public sealed class Wallet
{
    /// <summary>TR: Wallet kimliği. EN: Wallet identifier. Architecture: Aggregate Identity.</summary>
    public Guid Id { get; set; }
    /// <summary>TR: Wallet sahibi kullanıcı kimliği. EN: Owner user identifier. Architecture: Aggregate Reference.</summary>
    public Guid UserId { get; set; }
    /// <summary>TR: Kullanılabilir bakiye. EN: Available balance. Architecture: Aggregate State.</summary>
    public decimal Balance { get; set; }
    /// <summary>TR: ISO 4217 para birimi; ilk sürüm TRY. EN: ISO 4217 currency; TRY in v1. Architecture: Monetary Metadata.</summary>
    public string Currency { get; set; } = "TRY";
    /// <summary>TR: Wallet durumu. EN: Wallet status. Architecture: Explicit Domain State.</summary>
    public WalletStatus Status { get; set; }
    /// <summary>TR: Tek işlem kullanıcı limiti. EN: User-defined single transaction limit. Architecture: Domain Rule Input.</summary>
    public decimal SingleTransactionLimit { get; set; }
    /// <summary>TR: Günlük kullanıcı limiti. EN: User-defined daily limit. Architecture: Domain Rule Input.</summary>
    public decimal DailyLimit { get; set; }
    /// <summary>TR: Aylık kullanıcı limiti. EN: User-defined monthly limit. Architecture: Domain Rule Input.</summary>
    public decimal MonthlyLimit { get; set; }
    /// <summary>TR: UTC oluşturulma zamanı. EN: UTC creation time. Architecture: Audit Metadata.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>TR: UTC güncellenme zamanı. EN: UTC update time. Architecture: Audit Metadata.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

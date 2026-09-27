namespace RockBreaker.Pay.Modules.Wallet.Contracts;

/// <summary>
/// TR: Wallet özet bilgisini dış API'ye taşır.
/// EN: Carries wallet summary information to the external API.
/// Architecture: Response DTO.
/// </summary>
public sealed class WalletSummaryResponse
{
    /// <summary>TR: Wallet kimliği. EN: Wallet identifier. Architecture: DTO Property.</summary>
    public Guid Id { get; init; }
    /// <summary>TR: Kullanılabilir bakiye. EN: Available balance. Architecture: DTO Property.</summary>
    public decimal Balance { get; init; }
    /// <summary>TR: Para birimi. EN: Currency. Architecture: DTO Property.</summary>
    public string Currency { get; init; } = string.Empty;
    /// <summary>TR: Wallet durumu. EN: Wallet status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>TR: Tek işlem limiti. EN: Single transaction limit. Architecture: DTO Property.</summary>
    public decimal SingleTransactionLimit { get; init; }
    /// <summary>TR: Günlük limit. EN: Daily limit. Architecture: DTO Property.</summary>
    public decimal DailyLimit { get; init; }
    /// <summary>TR: Aylık limit. EN: Monthly limit. Architecture: DTO Property.</summary>
    public decimal MonthlyLimit { get; init; }
}

/// <summary>
/// TR: Kullanıcının wallet limitlerini güncelleme isteğidir.
/// EN: Request for updating user wallet limits.
/// Architecture: Request DTO.
/// </summary>
public sealed class UpdateWalletLimitsRequest
{
    /// <summary>TR: Tek işlem limiti. EN: Single transaction limit. Architecture: DTO Property.</summary>
    public decimal SingleTransactionLimit { get; init; }
    /// <summary>TR: Günlük limit. EN: Daily limit. Architecture: DTO Property.</summary>
    public decimal DailyLimit { get; init; }
    /// <summary>TR: Aylık limit. EN: Monthly limit. Architecture: DTO Property.</summary>
    public decimal MonthlyLimit { get; init; }
}

/// <summary>
/// TR: Wallet hareketini dış API'ye taşır.
/// EN: Carries a wallet movement to the external API.
/// Architecture: Response DTO.
/// </summary>
public sealed class WalletMovementResponse
{
    /// <summary>TR: İşlem kimliği. EN: Transaction identifier. Architecture: DTO Property.</summary>
    public Guid TransactionId { get; init; }
    /// <summary>TR: İşlem tipi. EN: Transaction type. Architecture: DTO Property.</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>TR: İşlem tutarı. EN: Transaction amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: Para birimi. EN: Currency. Architecture: DTO Property.</summary>
    public string Currency { get; init; } = string.Empty;
    /// <summary>TR: İşlem durumu. EN: Transaction status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>TR: Kullanıcı açısından Debit/Credit yönü. EN: Debit/Credit direction from user's perspective. Architecture: DTO Property.</summary>
    public string Direction { get; init; } = string.Empty;
    /// <summary>TR: UTC işlem zamanı. EN: UTC transaction time. Architecture: DTO Property.</summary>
    public DateTime CreatedAtUtc { get; init; }
}

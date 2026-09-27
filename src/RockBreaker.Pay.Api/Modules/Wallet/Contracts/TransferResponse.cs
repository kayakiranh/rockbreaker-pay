namespace RockBreaker.Pay.Modules.Wallet.Contracts;

/// <summary>
/// TR: Tamamlanan wallet transferinin dış API cevabıdır.
/// EN: External API response for a completed wallet transfer.
/// Architecture: Response DTO.
/// </summary>
public sealed class TransferResponse
{
    /// <summary>TR: Finansal işlem kimliği. EN: Financial transaction identifier. Architecture: DTO Property.</summary>
    public Guid TransactionId { get; init; }

    /// <summary>TR: Kaynak wallet işlem sonrası bakiye. EN: Source balance after transfer. Architecture: DTO Property.</summary>
    public decimal SourceBalance { get; init; }

    /// <summary>TR: Hedef wallet işlem sonrası bakiye. EN: Destination balance after transfer. Architecture: DTO Property.</summary>
    public decimal DestinationBalance { get; init; }

    /// <summary>TR: İşlem durumu. EN: Transaction status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;
}

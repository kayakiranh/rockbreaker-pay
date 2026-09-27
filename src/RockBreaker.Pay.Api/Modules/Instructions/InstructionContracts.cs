namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Wallet-to-wallet otomatik ödeme talimatı oluşturma isteğidir.
/// EN: Request for creating an automatic wallet-to-wallet payment instruction.
/// Architecture: Request DTO.
/// </summary>
public sealed class CreatePaymentInstructionRequest
{
    /// <summary>TR: Hedef wallet kimliği. EN: Destination wallet identifier. Architecture: DTO Property.</summary>
    public Guid DestinationWalletId { get; init; }
    /// <summary>TR: Transfer tutarı. EN: Transfer amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: Tekrar sıklığı. EN: Recurrence frequency. Architecture: DTO Property.</summary>
    public PaymentInstructionFrequency Frequency { get; init; }
    /// <summary>TR: İlk çalıştırma UTC zamanı. EN: First UTC execution time. Architecture: DTO Property.</summary>
    public DateTime FirstRunAtUtc { get; init; }
}

/// <summary>
/// TR: Otomatik talimat bilgisini API'ye taşır.
/// EN: Carries automatic payment instruction information to the API.
/// Architecture: Response DTO.
/// </summary>
public sealed class PaymentInstructionResponse
{
    /// <summary>TR: Talimat kimliği. EN: Instruction identifier. Architecture: DTO Property.</summary>
    public Guid Id { get; init; }
    /// <summary>TR: Kaynak wallet kimliği. EN: Source wallet identifier. Architecture: DTO Property.</summary>
    public Guid SourceWalletId { get; init; }
    /// <summary>TR: Hedef wallet kimliği. EN: Destination wallet identifier. Architecture: DTO Property.</summary>
    public Guid DestinationWalletId { get; init; }
    /// <summary>TR: Tutar. EN: Amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: Sıklık. EN: Frequency. Architecture: DTO Property.</summary>
    public string Frequency { get; init; } = string.Empty;
    /// <summary>TR: Durum. EN: Status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>TR: Sonraki çalışma UTC zamanı. EN: Next UTC execution time. Architecture: DTO Property.</summary>
    public DateTime? NextRunAtUtc { get; init; }
    /// <summary>TR: Son hata. EN: Last error. Architecture: DTO Property.</summary>
    public string? LastError { get; init; }
}

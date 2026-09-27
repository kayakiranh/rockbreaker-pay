namespace RockBreaker.Pay.Modules.Wallet.Contracts;

/// <summary>
/// TR: Wallet-to-wallet transfer isteğini taşır.
/// EN: Carries a wallet-to-wallet transfer request.
/// Architecture: Request DTO.
/// </summary>
public sealed class TransferRequest
{
    /// <summary>TR: Kaynak wallet kimliği. EN: Source wallet identifier. Architecture: DTO Property.</summary>
    public Guid SourceWalletId { get; init; }

    /// <summary>TR: Hedef wallet kimliği. EN: Destination wallet identifier. Architecture: DTO Property.</summary>
    public Guid DestinationWalletId { get; init; }

    /// <summary>TR: Transfer tutarı. EN: Transfer amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }

    /// <summary>TR: ISO 4217 para birimi; ilk sürümde TRY. EN: ISO 4217 currency; TRY in v1. Architecture: DTO Property.</summary>
    public string Currency { get; init; } = "TRY";
}

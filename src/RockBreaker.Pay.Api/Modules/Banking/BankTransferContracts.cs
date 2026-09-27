namespace RockBreaker.Pay.Modules.Banking;

/// <summary>
/// TR: Wallet ile bağlı fake banka hesabı arasındaki transfer isteğidir.
/// EN: Transfer request between the wallet and a linked fake bank account.
/// Architecture: Request DTO.
/// </summary>
public sealed class BankTransferRequest
{
    /// <summary>TR: Fake banka hesap kimliği. EN: Fake bank account identifier. Architecture: DTO Property.</summary>
    public Guid BankAccountId { get; init; }
    /// <summary>TR: Transfer tutarı. EN: Transfer amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
}

/// <summary>
/// TR: Banka transfer sonucunu API'ye taşır.
/// EN: Carries bank-transfer result to the API.
/// Architecture: Response DTO.
/// </summary>
public sealed class BankTransferResponse
{
    /// <summary>TR: Wallet transaction kimliği. EN: Wallet transaction identifier. Architecture: DTO Property.</summary>
    public Guid TransactionId { get; init; }
    /// <summary>TR: İşlem tipi. EN: Transaction type. Architecture: DTO Property.</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>TR: Tutar. EN: Amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: İşlem sonrası wallet bakiyesi. EN: Wallet balance after the transfer. Architecture: DTO Property.</summary>
    public decimal WalletBalance { get; init; }
}

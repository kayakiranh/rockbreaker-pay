namespace RockBreaker.Pay.Modules.Government;

/// <summary>
/// TR: Devlet SOAP servisinden gelen fatura bilgisini wallet API sözleşmesine taşır.
/// EN: Carries bill information from the government SOAP service into the wallet API contract.
/// Architecture: Anti-Corruption Response DTO.
/// </summary>
public sealed class BillResponse
{
    /// <summary>TR: Fatura kimliği. EN: Bill identifier. Architecture: DTO Property.</summary>
    public Guid Id { get; init; }

    /// <summary>TR: Faturayı oluşturan kurum. EN: Institution issuing the bill. Architecture: DTO Property.</summary>
    public string Institution { get; init; } = string.Empty;

    /// <summary>TR: Fatura tutarı. EN: Bill amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }

    /// <summary>TR: Faturanın ödenmiş olup olmadığı. EN: Whether the bill is paid. Architecture: DTO Property.</summary>
    public bool IsPaid { get; init; }
}

/// <summary>
/// TR: Fatura ödeme sonucunu temsil eder.
/// EN: Represents a bill-payment result.
/// Architecture: Response DTO.
/// </summary>
public sealed class BillPaymentResponse
{
    /// <summary>TR: Fatura kimliği. EN: Bill identifier. Architecture: DTO Property.</summary>
    public Guid BillId { get; init; }

    /// <summary>TR: Wallet finansal işlem kimliği. EN: Wallet financial transaction identifier. Architecture: DTO Property.</summary>
    public Guid TransactionId { get; init; }

    /// <summary>TR: Ödenen tutar. EN: Paid amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }

    /// <summary>TR: İşlem sonrası wallet bakiyesi. EN: Wallet balance after payment. Architecture: DTO Property.</summary>
    public decimal WalletBalance { get; init; }

    /// <summary>TR: Sonuç durumu. EN: Result status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;
}

namespace RockBreaker.Pay.Modules.MoneyRequests;

/// <summary>
/// TR: Başka bir wallet kullanıcısından para isteme isteğidir.
/// EN: Request for asking money from another wallet user.
/// Architecture: Request DTO.
/// </summary>
public sealed class CreateMoneyRequest
{
    /// <summary>TR: Paranın istendiği wallet. EN: Wallet from which money is requested. Architecture: DTO Property.</summary>
    public Guid RequestedFromWalletId { get; init; }
    /// <summary>TR: İstenen tutar. EN: Requested amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: Opsiyonel açıklama. EN: Optional description. Architecture: DTO Property.</summary>
    public string? Note { get; init; }
}

/// <summary>
/// TR: Borç/para isteği bilgisini API'ye taşır.
/// EN: Carries money-request information to the API.
/// Architecture: Response DTO.
/// </summary>
public sealed class MoneyRequestResponse
{
    /// <summary>TR: İstek kimliği. EN: Request identifier. Architecture: DTO Property.</summary>
    public Guid Id { get; init; }
    /// <summary>TR: İsteyen wallet. EN: Requester wallet. Architecture: DTO Property.</summary>
    public Guid RequesterWalletId { get; init; }
    /// <summary>TR: Paranın istendiği wallet. EN: Requested-from wallet. Architecture: DTO Property.</summary>
    public Guid RequestedFromWalletId { get; init; }
    /// <summary>TR: Tutar. EN: Amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: Durum. EN: Status. Architecture: DTO Property.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>TR: Açıklama. EN: Note. Architecture: DTO Property.</summary>
    public string? Note { get; init; }
    /// <summary>TR: UTC oluşturulma zamanı. EN: UTC creation time. Architecture: Audit Metadata.</summary>
    public DateTime CreatedAtUtc { get; init; }
    /// <summary>TR: UTC son geçerlilik zamanı. EN: UTC expiration time. Architecture: Lifecycle Metadata.</summary>
    public DateTime ExpiresAtUtc { get; init; }
}

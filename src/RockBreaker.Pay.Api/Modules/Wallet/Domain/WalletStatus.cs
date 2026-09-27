namespace RockBreaker.Pay.Modules.Wallet.Domain;

/// <summary>
/// TR: Wallet'ın operasyonel durumunu belirtir.
/// EN: Defines the operational state of a wallet.
/// Architecture: Explicit Domain State.
/// </summary>
public enum WalletStatus
{
    /// <summary>TR: İşlem yapabilir wallet. EN: Wallet can transact. Architecture: Domain State.</summary>
    Active = 1,
    /// <summary>TR: Geçici pasif wallet. EN: Temporarily passive wallet. Architecture: Domain State.</summary>
    Passive = 2,
    /// <summary>TR: Fraud veya operasyonel sebeple bloke wallet. EN: Wallet blocked for fraud or operational reasons. Architecture: Domain State.</summary>
    Blocked = 3,
    /// <summary>TR: Kalıcı kapatılmış wallet. EN: Permanently closed wallet. Architecture: Domain State.</summary>
    Closed = 4
}

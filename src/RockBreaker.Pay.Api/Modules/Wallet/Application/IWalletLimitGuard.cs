using System.Data;
using WalletEntity = RockBreaker.Pay.Modules.Wallet.Domain.Wallet;

namespace RockBreaker.Pay.Modules.Wallet.Application;

/// <summary>
/// TR: Wallet'ın tek işlem, günlük ve aylık kullanıcı limitlerini finansal transaction içinde kontrol eder.
/// EN: Checks wallet single-transaction, daily and monthly user limits inside the financial transaction.
/// Architecture: Domain Policy + Dependency Inversion.
/// </summary>
public interface IWalletLimitGuard
{
    /// <summary>
    /// TR: Yeni outgoing tutarın mevcut kullanım ile birlikte kullanıcı limitlerini aşıp aşmadığını kontrol eder.
    /// EN: Checks whether the new outgoing amount combined with current usage exceeds user limits.
    /// Architecture: Domain Policy executed within caller-owned Unit of Work.
    /// </summary>
    /// <param name="wallet">TR: Kaynak wallet. EN: Source wallet.</param>
    /// <param name="amount">TR: Yeni outgoing işlem tutarı. EN: New outgoing transaction amount.</param>
    /// <param name="connection">TR: Açık DB bağlantısı. EN: Open DB connection.</param>
    /// <param name="transaction">TR: Aktif SQL transaction. EN: Active SQL transaction.</param>
    /// <returns>TR: Limit kontrol sonucu. EN: Limit check result.</returns>
    Task<WalletLimitCheckResult> CheckAsync(
        WalletEntity wallet,
        decimal amount,
        IDbConnection connection,
        IDbTransaction transaction);
}

/// <summary>
/// TR: Wallet limit kontrolünün açıklanabilir sonucunu taşır.
/// EN: Carries the explainable result of a wallet limit check.
/// Architecture: Domain Policy Result.
/// </summary>
public sealed class WalletLimitCheckResult
{
    /// <summary>TR: İşleme izin verilip verilmediği. EN: Whether the operation is allowed. Architecture: Policy Result Property.</summary>
    public bool IsAllowed { get; init; }

    /// <summary>TR: Limit ihlalinde stabil hata kodu. EN: Stable error code for a limit violation. Architecture: Policy Result Property.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>TR: Limit ihlal açıklaması. EN: Limit violation explanation. Architecture: Policy Result Property.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// TR: İzin verilen limit sonucu üretir.
    /// EN: Creates an allowed limit result.
    /// Architecture: Factory Method.
    /// </summary>
    /// <returns>TR: Başarılı limit sonucu. EN: Successful limit result.</returns>
    public static WalletLimitCheckResult Allowed() => new() { IsAllowed = true };

    /// <summary>
    /// TR: Limit ihlali sonucu üretir.
    /// EN: Creates a limit-violation result.
    /// Architecture: Factory Method.
    /// </summary>
    /// <param name="code">TR: Hata kodu. EN: Error code.</param>
    /// <param name="message">TR: Hata mesajı. EN: Error message.</param>
    /// <returns>TR: Limit ihlal sonucu. EN: Limit violation result.</returns>
    public static WalletLimitCheckResult Rejected(string code, string message) => new()
    {
        IsAllowed = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}

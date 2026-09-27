using System.Data;
using RockBreaker.Pay.Modules.Wallet.Domain;
using WalletEntity = RockBreaker.Pay.Modules.Wallet.Domain.Wallet;

namespace RockBreaker.Pay.Modules.Wallet.Abstractions;

/// <summary>
/// TR: Wallet verisini Dapper/SQL detaylarından ayıran repository sözleşmesidir.
/// EN: Repository contract separating wallet data access from Dapper/SQL details.
/// Architecture: Repository Pattern + Dependency Inversion.
/// </summary>
public interface IWalletRepository
{
    /// <summary>TR: Wallet'ı kimliğine göre getirir. EN: Gets a wallet by identifier. Architecture: Repository Query.</summary>
    Task<WalletEntity?> GetByIdAsync(Guid walletId);

    /// <summary>TR: Kullanıcının wallet'ını getirir. EN: Gets a user's wallet. Architecture: Repository Query.</summary>
    Task<WalletEntity?> GetByUserIdAsync(Guid userId);

    /// <summary>TR: Yeni wallet ekler. EN: Inserts a new wallet. Architecture: Repository Command.</summary>
    Task InsertAsync(WalletEntity wallet);

    /// <summary>
    /// TR: Bakiye değişikliği sırasında wallet satırını update lock ile getirir.
    /// EN: Gets the wallet using an update lock during a balance-changing operation.
    /// Architecture: Pessimistic Locking against double-spend races.
    /// </summary>
    Task<WalletEntity?> GetForUpdateAsync(Guid walletId, IDbConnection connection, IDbTransaction transaction);

    /// <summary>TR: Wallet finansal durumunu transaction içinde günceller. EN: Updates wallet financial state inside the transaction. Architecture: Repository Command.</summary>
    Task UpdateAsync(WalletEntity wallet, IDbConnection connection, IDbTransaction transaction);

    /// <summary>TR: Wallet durumunu günceller. EN: Updates wallet status. Architecture: Repository Command.</summary>
    Task UpdateStatusAsync(Guid walletId, WalletStatus status);

    /// <summary>TR: Kullanıcı limitlerini günceller. EN: Updates user limits. Architecture: Repository Command.</summary>
    Task UpdateLimitsAsync(WalletEntity wallet);

    /// <summary>TR: Wallet'a ait finansal işlemleri döndürür. EN: Returns financial transactions for a wallet. Architecture: Repository Query.</summary>
    Task<IReadOnlyCollection<WalletTransaction>> GetTransactionsAsync(Guid walletId);
}

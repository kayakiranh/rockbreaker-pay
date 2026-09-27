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
    /// <summary>
    /// TR: Wallet'ı kimliğine göre getirir.
    /// EN: Gets a wallet by identifier.
    /// Architecture: Repository Query.
    /// </summary>
    /// <param name="walletId">TR: Wallet kimliği. EN: Wallet identifier.</param>
    /// <returns>TR: Wallet veya null. EN: Wallet or null.</returns>
    Task<WalletEntity?> GetByIdAsync(Guid walletId);

    /// <summary>
    /// TR: Bakiye değişikliği sırasında wallet satırını update lock ile getirir.
    /// EN: Gets the wallet using an update lock during a balance-changing operation.
    /// Architecture: Pessimistic Locking against double-spend races.
    /// </summary>
    /// <param name="walletId">TR: Wallet kimliği. EN: Wallet identifier.</param>
    /// <param name="connection">TR: Açık bağlantı. EN: Open connection.</param>
    /// <param name="transaction">TR: Aktif SQL transaction. EN: Active SQL transaction.</param>
    /// <returns>TR: Wallet veya null. EN: Wallet or null.</returns>
    Task<WalletEntity?> GetForUpdateAsync(Guid walletId, IDbConnection connection, IDbTransaction transaction);

    /// <summary>
    /// TR: Wallet finansal durumunu transaction içinde günceller.
    /// EN: Updates wallet financial state inside the transaction.
    /// Architecture: Repository Command + Unit of Work boundary owned by caller.
    /// </summary>
    /// <param name="wallet">TR: Wallet. EN: Wallet.</param>
    /// <param name="connection">TR: Açık bağlantı. EN: Open connection.</param>
    /// <param name="transaction">TR: Aktif transaction. EN: Active transaction.</param>
    Task UpdateAsync(WalletEntity wallet, IDbConnection connection, IDbTransaction transaction);

    /// <summary>
    /// TR: Wallet durumunu günceller; fraud sonrası block için kullanılır.
    /// EN: Updates wallet status; used to block after fraud.
    /// Architecture: Repository Command.
    /// </summary>
    /// <param name="walletId">TR: Wallet kimliği. EN: Wallet identifier.</param>
    /// <param name="status">TR: Yeni durum. EN: New status.</param>
    Task UpdateStatusAsync(Guid walletId, WalletStatus status);
}

using RockBreaker.Pay.Modules.Identity.Domain;

namespace RockBreaker.Pay.Modules.Identity.Abstractions;

/// <summary>
/// TR: Kullanıcı persistence işlemlerini application katmanından ayırır.
/// EN: Separates user persistence operations from the application layer.
/// Architecture: Repository Pattern + Dependency Inversion.
/// </summary>
public interface IUserRepository
{
    /// <summary>TR: E-posta ile kullanıcı getirir. EN: Gets a user by e-mail. Architecture: Repository Query.</summary>
    /// <param name="email">TR: E-posta. EN: E-mail.</param>
    /// <returns>TR: Kullanıcı veya null. EN: User or null.</returns>
    Task<UserAccount?> GetByEmailAsync(string email);

    /// <summary>TR: Kimlik ile kullanıcı getirir. EN: Gets a user by identifier. Architecture: Repository Query.</summary>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    /// <returns>TR: Kullanıcı veya null. EN: User or null.</returns>
    Task<UserAccount?> GetByIdAsync(Guid userId);

    /// <summary>TR: Yeni kullanıcı ekler. EN: Inserts a new user. Architecture: Repository Command.</summary>
    /// <param name="user">TR: Kullanıcı. EN: User.</param>
    Task InsertAsync(UserAccount user);

    /// <summary>TR: Profil alanlarını günceller. EN: Updates profile fields. Architecture: Repository Command.</summary>
    /// <param name="user">TR: Kullanıcı. EN: User.</param>
    Task UpdateProfileAsync(UserAccount user);

    /// <summary>TR: Parola hash'ini günceller. EN: Updates password hash. Architecture: Repository Command.</summary>
    /// <param name="userId">TR: Kullanıcı kimliği. EN: User identifier.</param>
    /// <param name="passwordHash">TR: Yeni hash. EN: New hash.</param>
    Task UpdatePasswordAsync(Guid userId, string passwordHash);
}

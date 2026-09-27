using RockBreaker.Pay.Modules.Identity.Contracts;
using RockBreaker.Pay.Modules.Identity.Domain;

namespace RockBreaker.Pay.Modules.Identity.Mapping;

/// <summary>
/// TR: UserAccount domain nesnesini dış ProfileResponse sözleşmesine dönüştürür.
/// EN: Maps the UserAccount domain object to the external ProfileResponse contract.
/// Architecture: Explicit Mapper Pattern; AutoMapper kullanılmaz.
/// </summary>
public interface IProfileMapper
{
    /// <summary>
    /// TR: UserAccount nesnesini ProfileResponse'a dönüştürür.
    /// EN: Maps UserAccount to ProfileResponse.
    /// Architecture: Explicit Mapping.
    /// </summary>
    /// <param name="user">TR: Domain kullanıcı nesnesi. EN: Domain user object.</param>
    /// <returns>TR: Profil cevabı. EN: Profile response.</returns>
    ProfileResponse Map(UserAccount user);
}

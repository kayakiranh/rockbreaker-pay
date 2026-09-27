using RockBreaker.Pay.Modules.Identity.Contracts;
using RockBreaker.Pay.Modules.Identity.Domain;

namespace RockBreaker.Pay.Modules.Identity.Mapping;

/// <summary>
/// TR: Profil dönüşümünü açık ve izlenebilir kod ile yapar.
/// EN: Performs profile mapping with explicit and traceable code.
/// Architecture: Explicit Mapper Pattern.
/// </summary>
public sealed class ProfileMapper : IProfileMapper
{
    /// <inheritdoc />
    public ProfileResponse Map(UserAccount user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Phone = user.Phone,
        KycStatus = user.KycStatus,
        SmsEnabled = user.SmsEnabled,
        EmailEnabled = user.EmailEnabled,
        PushEnabled = user.PushEnabled
    };
}

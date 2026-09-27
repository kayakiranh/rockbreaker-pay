namespace RockBreaker.Pay.Modules.Identity.Domain;

/// <summary>
/// TR: RockBreaker Pay kullanıcısının kimlik ve profil verilerini temsil eder.
/// EN: Represents identity and profile data of a RockBreaker Pay user.
/// Architecture: Domain Entity.
/// </summary>
public sealed class UserAccount
{
    /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: Entity Identity.</summary>
    public Guid Id { get; set; }
    /// <summary>TR: Benzersiz e-posta adresi. EN: Unique e-mail address. Architecture: Login Identifier.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>TR: PBKDF2 ile üretilmiş parola hash'i. EN: Password hash produced with PBKDF2. Architecture: Credential Storage.</summary>
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>TR: Kullanıcı adı. EN: First name. Architecture: Profile State.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>TR: Kullanıcı soyadı. EN: Last name. Architecture: Profile State.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>TR: Telefon numarası. EN: Phone number. Architecture: Profile State.</summary>
    public string? Phone { get; set; }
    /// <summary>TR: Kullanıcı rolü. EN: User role. Architecture: Role-Based Authorization.</summary>
    public string Role { get; set; } = "User";
    /// <summary>TR: KYC durumu. EN: KYC status. Architecture: Compliance State.</summary>
    public string KycStatus { get; set; } = "NotStarted";
    /// <summary>TR: SMS tercihi. EN: SMS preference. Architecture: Notification Preference.</summary>
    public bool SmsEnabled { get; set; } = true;
    /// <summary>TR: E-mail tercihi. EN: E-mail preference. Architecture: Notification Preference.</summary>
    public bool EmailEnabled { get; set; } = true;
    /// <summary>TR: Push tercihi. EN: Push preference. Architecture: Notification Preference.</summary>
    public bool PushEnabled { get; set; } = true;
    /// <summary>TR: UTC oluşturulma zamanı. EN: UTC creation time. Architecture: Audit Metadata.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>TR: UTC güncellenme zamanı. EN: UTC update time. Architecture: Audit Metadata.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

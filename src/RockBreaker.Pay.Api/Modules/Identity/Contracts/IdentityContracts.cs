namespace RockBreaker.Pay.Modules.Identity.Contracts;

/// <summary>
/// TR: Kullanıcı kayıt isteğidir.
/// EN: User registration request.
/// Architecture: Request DTO.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>TR: E-posta. EN: E-mail. Architecture: DTO Property.</summary>
    public string Email { get; init; } = string.Empty;
    /// <summary>TR: Parola. EN: Password. Architecture: DTO Property.</summary>
    public string Password { get; init; } = string.Empty;
    /// <summary>TR: Ad. EN: First name. Architecture: DTO Property.</summary>
    public string FirstName { get; init; } = string.Empty;
    /// <summary>TR: Soyad. EN: Last name. Architecture: DTO Property.</summary>
    public string LastName { get; init; } = string.Empty;
}

/// <summary>
/// TR: Kullanıcı giriş isteğidir.
/// EN: User login request.
/// Architecture: Request DTO.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>TR: E-posta. EN: E-mail. Architecture: DTO Property.</summary>
    public string Email { get; init; } = string.Empty;
    /// <summary>TR: Parola. EN: Password. Architecture: DTO Property.</summary>
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// TR: Refresh token isteğidir.
/// EN: Refresh token request.
/// Architecture: Request DTO.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>TR: Refresh token. EN: Refresh token. Architecture: DTO Property.</summary>
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// TR: Forgot-password isteğidir.
/// EN: Forgot-password request.
/// Architecture: Request DTO.
/// </summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>TR: E-posta. EN: E-mail. Architecture: DTO Property.</summary>
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// TR: Password reset isteğidir.
/// EN: Password reset request.
/// Architecture: Request DTO.
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>TR: Reset token. EN: Reset token. Architecture: DTO Property.</summary>
    public string Token { get; init; } = string.Empty;
    /// <summary>TR: Yeni parola. EN: New password. Architecture: DTO Property.</summary>
    public string NewPassword { get; init; } = string.Empty;
}

/// <summary>
/// TR: Profil güncelleme isteğidir.
/// EN: Profile update request.
/// Architecture: Request DTO.
/// </summary>
public sealed class UpdateProfileRequest
{
    /// <summary>TR: Ad. EN: First name. Architecture: DTO Property.</summary>
    public string FirstName { get; init; } = string.Empty;
    /// <summary>TR: Soyad. EN: Last name. Architecture: DTO Property.</summary>
    public string LastName { get; init; } = string.Empty;
    /// <summary>TR: Telefon. EN: Phone. Architecture: DTO Property.</summary>
    public string? Phone { get; init; }
    /// <summary>TR: SMS tercihi. EN: SMS preference. Architecture: DTO Property.</summary>
    public bool SmsEnabled { get; init; }
    /// <summary>TR: E-mail tercihi. EN: E-mail preference. Architecture: DTO Property.</summary>
    public bool EmailEnabled { get; init; }
    /// <summary>TR: Push tercihi. EN: Push preference. Architecture: DTO Property.</summary>
    public bool PushEnabled { get; init; }
}

/// <summary>
/// TR: Access/refresh token cevabıdır.
/// EN: Access/refresh token response.
/// Architecture: Response DTO.
/// </summary>
public sealed class TokenResponse
{
    /// <summary>TR: JWT access token. EN: JWT access token. Architecture: Token Contract.</summary>
    public string AccessToken { get; init; } = string.Empty;
    /// <summary>TR: Refresh token. EN: Refresh token. Architecture: Token Contract.</summary>
    public string RefreshToken { get; init; } = string.Empty;
    /// <summary>TR: Access token UTC son kullanma zamanı. EN: Access-token UTC expiration. Architecture: Token Metadata.</summary>
    public DateTime ExpiresAtUtc { get; init; }
}

/// <summary>
/// TR: Kullanıcı profil cevabıdır.
/// EN: User profile response.
/// Architecture: Response DTO.
/// </summary>
public sealed class ProfileResponse
{
    /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: DTO Property.</summary>
    public Guid Id { get; init; }
    /// <summary>TR: E-posta. EN: E-mail. Architecture: DTO Property.</summary>
    public string Email { get; init; } = string.Empty;
    /// <summary>TR: Ad. EN: First name. Architecture: DTO Property.</summary>
    public string FirstName { get; init; } = string.Empty;
    /// <summary>TR: Soyad. EN: Last name. Architecture: DTO Property.</summary>
    public string LastName { get; init; } = string.Empty;
    /// <summary>TR: Telefon. EN: Phone. Architecture: DTO Property.</summary>
    public string? Phone { get; init; }
    /// <summary>TR: KYC durumu. EN: KYC status. Architecture: Compliance DTO Property.</summary>
    public string KycStatus { get; init; } = string.Empty;
    /// <summary>TR: SMS tercihi. EN: SMS preference. Architecture: DTO Property.</summary>
    public bool SmsEnabled { get; init; }
    /// <summary>TR: E-mail tercihi. EN: E-mail preference. Architecture: DTO Property.</summary>
    public bool EmailEnabled { get; init; }
    /// <summary>TR: Push tercihi. EN: Push preference. Architecture: DTO Property.</summary>
    public bool PushEnabled { get; init; }
}

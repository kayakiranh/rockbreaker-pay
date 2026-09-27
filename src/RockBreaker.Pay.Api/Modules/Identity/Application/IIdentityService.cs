using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Identity.Contracts;

namespace RockBreaker.Pay.Modules.Identity.Application;

/// <summary>
/// TR: Kayıt, giriş, token yenileme, forgot/reset password ve profil use-case'lerini tanımlar.
/// EN: Defines registration, login, token refresh, forgot/reset password and profile use cases.
/// Architecture: Application Service Interface.
/// </summary>
public interface IIdentityService
{
    /// <summary>TR: Kullanıcı kaydeder. EN: Registers a user. Architecture: Application Command.</summary>
    Task<OperationResult<TokenResponse>> RegisterAsync(RegisterRequest request);
    /// <summary>TR: Kullanıcı girişi yapar. EN: Logs a user in. Architecture: Application Command.</summary>
    Task<OperationResult<TokenResponse>> LoginAsync(LoginRequest request);
    /// <summary>TR: Token yeniler. EN: Refreshes tokens. Architecture: Application Command.</summary>
    Task<OperationResult<TokenResponse>> RefreshAsync(RefreshTokenRequest request);
    /// <summary>TR: Reset token üretir. EN: Creates a reset token. Architecture: Application Command.</summary>
    Task<OperationResult<string>> ForgotPasswordAsync(ForgotPasswordRequest request);
    /// <summary>TR: Parolayı resetler. EN: Resets the password. Architecture: Application Command.</summary>
    Task<OperationResult<bool>> ResetPasswordAsync(ResetPasswordRequest request);
    /// <summary>TR: Profil getirir. EN: Gets profile. Architecture: Application Query.</summary>
    Task<OperationResult<ProfileResponse>> GetProfileAsync(Guid userId);
    /// <summary>TR: Profil günceller. EN: Updates profile. Architecture: Application Command.</summary>
    Task<OperationResult<ProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
}

using RockBreaker.Pay.Modules.Identity.Contracts;
using RockBreaker.Pay.Modules.Identity.Domain;

namespace RockBreaker.Pay.Modules.Identity.Security;

/// <summary>
/// TR: JWT access token ve refresh token üretimini soyutlar.
/// EN: Abstracts JWT access-token and refresh-token generation.
/// Architecture: Token Service + Dependency Inversion.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// TR: Kullanıcı için yeni access/refresh token çifti üretir ve refresh token hash'ini saklar.
    /// EN: Creates a new access/refresh token pair and persists the refresh-token hash.
    /// Architecture: JWT + Refresh Token Rotation.
    /// </summary>
    /// <param name="user">TR: Kullanıcı. EN: User.</param>
    /// <returns>TR: Token cevabı. EN: Token response.</returns>
    Task<TokenResponse> CreateAsync(UserAccount user);

    /// <summary>
    /// TR: Geçerli refresh token ile yeni token çifti üretir.
    /// EN: Creates a new token pair from a valid refresh token.
    /// Architecture: Refresh Token Rotation.
    /// </summary>
    /// <param name="refreshToken">TR: Refresh token. EN: Refresh token.</param>
    /// <returns>TR: Yeni token cevabı veya null. EN: New token response or null.</returns>
    Task<TokenResponse?> RefreshAsync(string refreshToken);
}

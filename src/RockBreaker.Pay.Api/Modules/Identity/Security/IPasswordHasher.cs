namespace RockBreaker.Pay.Modules.Identity.Security;

/// <summary>
/// TR: Parola hashleme ve doğrulamayı soyutlar.
/// EN: Abstracts password hashing and verification.
/// Architecture: Strategy Pattern + Dependency Inversion.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// TR: Parolayı güvenli tek-yönlü PBKDF2 hash'e dönüştürür.
    /// EN: Converts a password into a secure one-way PBKDF2 hash.
    /// Architecture: Password Hashing Strategy.
    /// </summary>
    /// <param name="password">TR: Ham parola. EN: Raw password.</param>
    /// <returns>TR: Saklanabilir hash. EN: Persistable hash.</returns>
    string Hash(string password);

    /// <summary>
    /// TR: Parolayı saklanan hash ile sabit-zamanlı karşılaştırır.
    /// EN: Verifies a password against the stored hash using constant-time comparison.
    /// Architecture: Password Verification Strategy.
    /// </summary>
    /// <param name="password">TR: Ham parola. EN: Raw password.</param>
    /// <param name="storedHash">TR: Saklanan hash. EN: Stored hash.</param>
    /// <returns>TR: Doğrulama sonucu. EN: Verification result.</returns>
    bool Verify(string password, string storedHash);
}

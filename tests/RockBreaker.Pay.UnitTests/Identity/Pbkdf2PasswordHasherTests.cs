using RockBreaker.Pay.Modules.Identity.Security;

namespace RockBreaker.Pay.UnitTests.Identity;

/// <summary>
/// TR: PBKDF2 parola hashleme stratejisinin temel güvenlik davranışlarını test eder.
/// EN: Tests core security behavior of the PBKDF2 password hashing strategy.
/// Architecture: Unit Test for Security Strategy.
/// </summary>
public sealed class Pbkdf2PasswordHasherTests
{
    /// <summary>
    /// TR: Hashlenen doğru parolanın doğrulandığını test eder.
    /// EN: Verifies that the correct password matches its stored hash.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Verify_CorrectPassword_ShouldReturnTrue()
    {
        var hasher = new Pbkdf2PasswordHasher();
        const string password = "StrongPassword123!";

        var hash = hasher.Hash(password);
        var result = hasher.Verify(password, hash);

        Assert.True(result);
    }

    /// <summary>
    /// TR: Yanlış parolanın aynı hash ile doğrulanamadığını test eder.
    /// EN: Verifies that an incorrect password does not match the stored hash.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Verify_WrongPassword_ShouldReturnFalse()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hash = hasher.Hash("CorrectPassword123!");

        var result = hasher.Verify("WrongPassword123!", hash);

        Assert.False(result);
    }

    /// <summary>
    /// TR: Aynı parolanın random salt nedeniyle farklı hash'ler ürettiğini doğrular.
    /// EN: Verifies that the same password produces different hashes because of random salt.
    /// Architecture: Password Salt Behavior Test.
    /// </summary>
    [Fact]
    public void Hash_SamePasswordTwice_ShouldProduceDifferentHashes()
    {
        var hasher = new Pbkdf2PasswordHasher();

        var first = hasher.Hash("StrongPassword123!");
        var second = hasher.Hash("StrongPassword123!");

        Assert.NotEqual(first, second);
    }
}

using RockBreaker.Pay.Infrastructure.Auditing;

namespace RockBreaker.Pay.UnitTests.Auditing;

/// <summary>
/// TR: Merkezi log maskeleme politikasını test eder.
/// EN: Tests the centralized log masking policy.
/// Architecture: Unit Test for Cross-Cutting Security Concern.
/// </summary>
public sealed class SensitiveDataMaskerTests
{
    /// <summary>
    /// TR: Password alanının maskelendiğini doğrular.
    /// EN: Verifies that the password field is masked.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Mask_Password_ShouldHideValue()
    {
        const string input = """{"email":"user@test.com","password":"Secret123!"}""";

        var result = SensitiveDataMasker.Mask(input);

        Assert.DoesNotContain("Secret123!", result);
        Assert.Contains("***MASKED***", result);
    }

    /// <summary>
    /// TR: Metin içindeki 16 haneli sayının yalnız ilk ve son dört hanesini bıraktığını doğrular.
    /// EN: Verifies that a 16-digit number keeps only its first and last four digits.
    /// Architecture: Arrange-Act-Assert.
    /// </summary>
    [Fact]
    public void Mask_SixteenDigitNumber_ShouldMaskMiddleDigits()
    {
        const string input = "card=1111222233334444";

        var result = SensitiveDataMasker.Mask(input);

        Assert.Contains("1111********4444", result);
        Assert.DoesNotContain("1111222233334444", result);
    }
}

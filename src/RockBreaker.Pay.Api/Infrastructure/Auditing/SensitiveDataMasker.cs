using System.Text.RegularExpressions;

namespace RockBreaker.Pay.Infrastructure.Auditing;

/// <summary>
/// TR: Audit loglarına girmemesi gereken parola, token ve kart benzeri hassas verileri maskeler.
/// EN: Masks password, token and card-like sensitive data that must not enter audit logs.
/// Architecture: Centralized Data Protection Policy.
/// </summary>
public static partial class SensitiveDataMasker
{
    /// <summary>
    /// TR: Metin içindeki hassas JSON alanlarını ve 16 haneli sayıları maskeler.
    /// EN: Masks sensitive JSON fields and 16-digit numbers inside text.
    /// Architecture: Centralized Logging Sanitizer.
    /// </summary>
    /// <param name="value">TR: Ham log metni. EN: Raw log text.</param>
    /// <returns>TR: Maskelenmiş metin. EN: Masked text.</returns>
    public static string? Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var masked = SensitiveJsonFieldRegex().Replace(value, "$1***MASKED***$3");
        return SixteenDigitRegex().Replace(masked, match =>
            $"{match.Value[..4]}********{match.Value[^4..]}");
    }

    [GeneratedRegex("""("(?:password|accessToken|refreshToken|otp|token)"\s*:\s*")(.*?)(")""", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveJsonFieldRegex();

    [GeneratedRegex("""(?<!\d)\d{16}(?!\d)""")]
    private static partial Regex SixteenDigitRegex();
}

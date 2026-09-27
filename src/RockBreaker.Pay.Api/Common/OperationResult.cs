namespace RockBreaker.Pay.Common;

/// <summary>
/// TR: Application katmanındaki işlemlerin başarılı veya başarısız sonucunu exception kullanmadan taşır.
/// EN: Carries successful or failed application results without using exceptions for business outcomes.
/// Architecture: Result Pattern.
/// </summary>
/// <typeparam name="T">TR: Başarılı sonuç tipi. EN: Successful result type.</typeparam>
public sealed class OperationResult<T>
{
    /// <summary>TR: İşlemin başarılı olup olmadığını belirtir. EN: Indicates whether the operation succeeded. Architecture: Result Pattern.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>TR: Başarılı işlem verisi. EN: Successful operation data. Architecture: Result Pattern.</summary>
    public T? Data { get; init; }

    /// <summary>TR: Makine tarafından okunabilir hata kodu. EN: Machine-readable error code. Architecture: Stable Error Contract.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>TR: Hata açıklaması. EN: Error description. Architecture: Stable Error Contract.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// TR: Başarılı sonuç üretir.
    /// EN: Creates a successful result.
    /// Architecture: Factory Method.
    /// </summary>
    /// <param name="data">TR: Sonuç verisi. EN: Result data.</param>
    /// <returns>TR: Başarılı sonuç. EN: Successful result.</returns>
    public static OperationResult<T> Success(T data) => new() { IsSuccess = true, Data = data };

    /// <summary>
    /// TR: Başarısız sonuç üretir.
    /// EN: Creates a failed result.
    /// Architecture: Factory Method.
    /// </summary>
    /// <param name="code">TR: Hata kodu. EN: Error code.</param>
    /// <param name="message">TR: Hata mesajı. EN: Error message.</param>
    /// <returns>TR: Başarısız sonuç. EN: Failed result.</returns>
    public static OperationResult<T> Fail(string code, string message) =>
        new() { IsSuccess = false, ErrorCode = code, ErrorMessage = message };
}

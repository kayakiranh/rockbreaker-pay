namespace RockBreaker.Pay.Modules.Cutoff;

/// <summary>
/// TR: Banka ile ilişkili işlemlerin mesai/tatil uygunluğunu dış FakeCutoff servisine sorar.
/// EN: Asks the external FakeCutoff service whether bank-related operations are allowed by working-hours/holiday rules.
/// Architecture: Anti-Corruption Layer + HTTP Client Adapter.
/// </summary>
public interface ICutoffClient
{
    /// <summary>
    /// TR: İşlem tipinin şu anda çalıştırılabilir olup olmadığını döndürür.
    /// EN: Returns whether the operation type can be executed now.
    /// Architecture: External Policy Adapter.
    /// </summary>
    /// <param name="operationType">TR: İşlem tipi. EN: Operation type.</param>
    /// <returns>TR: Uygunluk bilgisi. EN: Availability information.</returns>
    Task<bool> IsOperationAllowedAsync(string operationType);
}

namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Zamanı gelen otomatik talimatların tek batch çalıştırmasını tanımlar.
/// EN: Defines one batch execution for due automatic instructions.
/// Architecture: Background Job Processor Interface.
/// </summary>
public interface IPaymentInstructionProcessor
{
    /// <summary>
    /// TR: Zamanı gelen aktif talimatları işler.
    /// EN: Processes active instructions whose execution time has arrived.
    /// Architecture: Background Processing Command.
    /// </summary>
    /// <param name="cancellationToken">TR: İptal token'ı. EN: Cancellation token.</param>
    Task ProcessDueAsync(CancellationToken cancellationToken);
}

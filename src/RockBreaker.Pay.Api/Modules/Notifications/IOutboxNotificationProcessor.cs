namespace RockBreaker.Pay.Modules.Notifications;

/// <summary>
/// TR: Transactional outbox içindeki notification event'lerini tek batch halinde işler.
/// EN: Processes notification events from the transactional outbox in one batch.
/// Architecture: Transactional Outbox Processor.
/// </summary>
public interface IOutboxNotificationProcessor
{
    /// <summary>
    /// TR: İşlenmemiş notification event'lerini gönderir ve sonuçlarını outbox tablosuna yazar.
    /// EN: Sends unprocessed notification events and writes results back to the outbox table.
    /// Architecture: At-Least-Once Background Processing.
    /// </summary>
    /// <param name="cancellationToken">TR: İptal token'ı. EN: Cancellation token.</param>
    Task ProcessAsync(CancellationToken cancellationToken);
}

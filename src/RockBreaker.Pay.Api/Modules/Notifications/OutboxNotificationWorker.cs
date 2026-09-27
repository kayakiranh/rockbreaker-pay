namespace RockBreaker.Pay.Modules.Notifications;

/// <summary>
/// TR: Transactional outbox notification processor'ını periyodik olarak çalıştırır.
/// EN: Periodically executes the transactional-outbox notification processor.
/// Architecture: BackgroundService + Scoped Worker Pattern.
/// </summary>
public sealed class OutboxNotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// TR: Scoped processor oluşturmak için scope factory alır.
    /// EN: Receives a scope factory for creating scoped processors.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="scopeFactory">TR: Scope factory. EN: Scope factory.</param>
    public OutboxNotificationWorker(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    /// <summary>
    /// TR: Uygulama çalışırken outbox'ı periyodik olarak işler.
    /// EN: Periodically processes the outbox while the application is running.
    /// Architecture: Hosted Background Worker.
    /// </summary>
    /// <param name="stoppingToken">TR: Kapanış token'ı. EN: Shutdown token.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IOutboxNotificationProcessor>();
            await processor.ProcessAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

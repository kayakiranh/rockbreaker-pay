namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Otomatik talimat processor'ını periyodik olarak tetikleyen hosted service'tir.
/// EN: Hosted service periodically triggering the automatic-instruction processor.
/// Architecture: BackgroundService + Scoped Worker Pattern.
/// </summary>
public sealed class PaymentInstructionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>TR: Scope factory bağımlılığını alır. EN: Receives scope-factory dependency. Architecture: Constructor Injection.</summary>
    public PaymentInstructionWorker(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    /// <summary>
    /// TR: Uygulama yaşadığı sürece dakikada bir zamanı gelen talimatları işler.
    /// EN: Processes due instructions once per minute while the application is running.
    /// Architecture: Hosted Background Worker.
    /// </summary>
    /// <param name="stoppingToken">TR: Uygulama kapanış token'ı. EN: Application shutdown token.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IPaymentInstructionProcessor>();
            await processor.ProcessDueAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

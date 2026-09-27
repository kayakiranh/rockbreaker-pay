using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.CustomerJourney;

/// <summary>
/// TR: Otomatik ödeme talimatı oluşturma ve worker tarafından başarılı çalıştırılma journey'sini dokümante eder.
/// EN: Documents automatic payment instruction creation and successful worker execution journey.
/// Architecture: Read-Only API Journey Controller.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/customer-journey/automatic-payment")]
public sealed class AutomaticPaymentJourneyController : ControllerBase
{
    /// <summary>TR: Otomatik ödeme journey'sini success request örnekleriyle döndürür. EN: Returns the automatic-payment journey with successful request examples. Architecture: Documentation Query.</summary>
    [HttpGet]
    public ActionResult<CustomerJourneyResponse> Get()
    {
        return Ok(new CustomerJourneyResponse
        {
            Title = "Register -> KYC -> Fund Wallet -> Create Automatic Payment -> Worker Transfer",
            Notes =
            [
                "Talimat oluştururken kaynak ve hedef wallet aktif, iki taraf KYC Verified olmalıdır.",
                "Fraud kararı talimat oluşturma anında değil, gerçek para hareketinin gerçekleştiği worker execution anında alınır.",
                "Worker atomik DB claim kullanır; her occurrence için deterministik idempotency key üretir ve core WalletTransferService'i çağırır.",
                "Başarılı execution ledger + outbox oluşturur; FakeNotification worker tarafından gönderilir."
            ],
            Steps =
            [
                new CustomerJourneyStep
                {
                    Order = 1,
                    Name = "Prepare verified funded source wallet",
                    Service = "RockBreaker.Pay.Api -> FakeKyc -> FakeBanking",
                    Method = "POST",
                    Endpoint = "/api/auth/register -> /api/kyc/submit -> /api/wallets -> /api/bank-transfers/bank-to-wallet",
                    RequestBody = new { fundingAmount = 5000.00m },
                    ExpectedSuccess = "Aktif, Verified KYC sahibi ve yeterli bakiyeli source wallet.",
                    Integrations = ["Identity", "KYC", "FakeCutoff", "Fraud Engine", "FakeBanking", "Ledger", "Audit"],
                    Architecture = "Composed Customer Onboarding Flow"
                },
                new CustomerJourneyStep
                {
                    Order = 2,
                    Name = "Create automatic payment instruction",
                    Service = "RockBreaker.Pay.Api",
                    Method = "POST",
                    Endpoint = "/api/payment-instructions",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Bearer(),
                    RequestBody = new { destinationWalletId = "<verified-active-destination-wallet-id>", amount = 250.00m, frequency = 2, firstRunAtUtc = "<utc-date-time>" },
                    ExpectedSuccess = "200 OK; instruction Active olur. frequency=2 Daily anlamına gelir.",
                    Integrations = ["JWT", "Source KYC", "Destination KYC", "Source/Destination Wallet Active Guard", "Dapper Persistence", "Audit"],
                    Architecture = "Application Service + Explicit State Model"
                },
                new CustomerJourneyStep
                {
                    Order = 3,
                    Name = "Worker claims due instruction",
                    Service = "PaymentInstructionWorker",
                    Method = "INTERNAL",
                    Endpoint = "PaymentInstructionProcessor.ProcessDueAsync",
                    ExpectedSuccess = "Due instruction Active -> Processing olarak atomik claim edilir.",
                    Integrations = ["BackgroundService", "UPDLOCK", "READPAST", "ROWLOCK", "Processing Lease"],
                    Architecture = "Database Work Queue + Distributed Claim"
                },
                new CustomerJourneyStep
                {
                    Order = 4,
                    Name = "Execute automatic transfer",
                    Service = "PaymentInstructionProcessor -> WalletTransferService",
                    Method = "INTERNAL",
                    Endpoint = "IWalletTransferService.TransferAsync",
                    ExpectedSuccess = "Transfer Completed; recurring instruction yeniden Active ve nextRunAtUtc hesaplanır.",
                    Integrations = ["Source KYC", "Destination KYC", "Fraud Engine", "Wallet Limits", "Occurrence Idempotency", "Pessimistic Locking", "Double-Entry Ledger", "Transactional Outbox", "FakeNotification", "Bounded Retry"],
                    Architecture = "Background Processor + Idempotent Financial Command"
                },
                new CustomerJourneyStep
                {
                    Order = 5,
                    Name = "Verify instruction state",
                    Service = "RockBreaker.Pay.Api",
                    Method = "GET",
                    Endpoint = "/api/payment-instructions",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Bearer(),
                    ExpectedSuccess = "200 OK; LastError null, FailureCount=0 ve sonraki çalışma zamanı görünür.",
                    Integrations = ["JWT", "Dapper Read Model", "Audit"],
                    Architecture = "REST Query"
                }
            ]
        });
    }
}

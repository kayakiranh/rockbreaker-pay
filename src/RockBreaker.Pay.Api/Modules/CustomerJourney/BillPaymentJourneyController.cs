using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.CustomerJourney;

/// <summary>
/// TR: Register'dan başarılı fatura ödemesine kadar success endpoint akışını dokümante eder.
/// EN: Documents the successful endpoint flow from registration to bill payment.
/// Architecture: Read-Only API Journey Controller.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/customer-journey/bill-payment")]
public sealed class BillPaymentJourneyController : ControllerBase
{
    /// <summary>TR: Fatura ödeme journey'sini success request örnekleriyle döndürür. EN: Returns the bill-payment journey with successful request examples. Architecture: Documentation Query.</summary>
    [HttpGet]
    public ActionResult<CustomerJourneyResponse> Get()
    {
        return Ok(new CustomerJourneyResponse
        {
            Title = "Register -> KYC -> Fund Wallet -> Government Bill Payment",
            Notes =
            [
                "Fatura ödeme öncesinde wallet sahibi Verified KYC olmalıdır.",
                "Fatura FakeGovernment SOAP servisinden sorgulanır; ödeme öncesi Fraud ve wallet limitleri uygulanır.",
                "SOAP ödeme başarısızsa wallet debit reversal ile telafi edilir; başarılıysa BillPaymentCompleted outbox event'i notification worker tarafından işlenir."
            ],
            Steps =
            [
                new CustomerJourneyStep
                {
                    Order = 1,
                    Name = "Register and verify KYC",
                    Service = "RockBreaker.Pay.Api -> FakeKyc",
                    Method = "POST",
                    Endpoint = "/api/auth/register + /api/kyc/submit",
                    Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                    RequestBody = new { register = new { email = "bill.customer@rockbreaker.local", password = "Customer123!", firstName = "Bill", lastName = "Customer" }, then = "POST /api/kyc/submit with Bearer token" },
                    ExpectedSuccess = "Register 200 OK ve KYC Verified.",
                    Integrations = ["Identity", "JWT", "KYC Provider", "Audit"],
                    Architecture = "Identity + Compliance Application Services"
                },
                new CustomerJourneyStep
                {
                    Order = 2,
                    Name = "Create and fund wallet",
                    Service = "RockBreaker.Pay.Api -> FakeCutoff -> Fraud -> FakeBanking",
                    Method = "POST",
                    Endpoint = "/api/wallets then /api/bank-transfers/bank-to-wallet",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Financial("bill-funding-001"),
                    RequestBody = new { bankAccountId = "<fake-bank-account-id>", amount = 2000.00m },
                    ExpectedSuccess = "Wallet aktif ve fatura tutarını karşılayacak bakiye ile hazırdır.",
                    Integrations = ["KYC", "FakeCutoff", "Fraud Engine", "FakeBanking", "Ledger", "Idempotency", "Audit"],
                    Architecture = "Wallet + Bank Transfer Saga"
                },
                new CustomerJourneyStep
                {
                    Order = 3,
                    Name = "List open government bills",
                    Service = "RockBreaker.Pay.Api -> FakeGovernment SOAP",
                    Method = "GET",
                    Endpoint = "/api/bills?citizenNumber=11111111111",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Bearer(),
                    ExpectedSuccess = "200 OK; açık faturalar döner ve seçilen billId sonraki adımda kullanılır.",
                    Integrations = ["JWT", "Government SOAP", "SOAP Anti-Corruption Layer", "Audit"],
                    Architecture = "REST-to-SOAP Adapter"
                },
                new CustomerJourneyStep
                {
                    Order = 4,
                    Name = "Pay government bill",
                    Service = "RockBreaker.Pay.Api -> Fraud -> FakeGovernment SOAP -> Outbox -> FakeNotification",
                    Method = "POST",
                    Endpoint = "/api/bills/<bill-id>/pay?citizenNumber=11111111111",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Financial("bill-payment-001"),
                    ExpectedSuccess = "200 OK; bill paid, wallet debit/ledger Completed ve notification outbox hazır olur.",
                    Integrations = ["JWT", "KYC", "Government SOAP", "Fraud Engine", "Wallet Limits", "Idempotency", "Pessimistic Locking", "Ledger", "Saga Compensation", "Transactional Outbox", "FakeNotification", "Audit"],
                    Architecture = "Saga Orchestration + Compensation + Transactional Outbox"
                }
            ]
        });
    }
}

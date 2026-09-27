using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.CustomerJourney;

/// <summary>
/// TR: Register'dan başarılı wallet-to-wallet transfere kadar success endpoint akışını dokümante eder.
/// EN: Documents the successful endpoint flow from registration to wallet-to-wallet transfer.
/// Architecture: Read-Only API Journey Controller.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/customer-journey/transfer")]
public sealed class TransferJourneyController : ControllerBase
{
    /// <summary>TR: Transfer journey'sini success request örnekleriyle döndürür. EN: Returns the transfer journey with successful request examples. Architecture: Documentation Query.</summary>
    [HttpGet]
    public ActionResult<CustomerJourneyResponse> Get()
    {
        return Ok(new CustomerJourneyResponse
        {
            Title = "Register -> KYC -> Fund Wallet -> Wallet Transfer",
            Notes =
            [
                "Kaynak ve hedef wallet aktif olmalı; iki wallet sahibinin de KYC durumu Verified olmalıdır.",
                "Para yükleme sırasında FakeCutoff + Fraud + FakeBanking; wallet transferinde Fraud + limits + ledger + outbox çalışır.",
                "Her finansal command için benzersiz Idempotency-Key kullanılmalıdır.",
                "RequestAuditMiddleware tüm HTTP çağrılarını maskelenmiş MSSQL audit kaydına ve Elasticsearch projection'a taşır."
            ],
            Steps =
            [
                new CustomerJourneyStep
                {
                    Order = 1,
                    Name = "Register customer",
                    Service = "RockBreaker.Pay.Api",
                    Method = "POST",
                    Endpoint = "/api/auth/register",
                    Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                    RequestBody = new { email = "customer@rockbreaker.local", password = "Customer123!", firstName = "Customer", lastName = "One" },
                    ExpectedSuccess = "200 OK; accessToken ve refreshToken döner.",
                    Integrations = ["Identity", "PBKDF2", "JWT", "Audit"],
                    Architecture = "Identity Application Service"
                },
                new CustomerJourneyStep
                {
                    Order = 2,
                    Name = "Verify KYC",
                    Service = "RockBreaker.Pay.Api -> FakeKyc",
                    Method = "POST",
                    Endpoint = "/api/kyc/submit",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Bearer(),
                    ExpectedSuccess = "200 OK; normal fake provider senaryosunda KycStatus=Verified.",
                    Integrations = ["JWT", "KYC Provider", "KycEvents Audit", "Audit"],
                    Architecture = "Compliance Service + Anti-Corruption Layer"
                },
                new CustomerJourneyStep
                {
                    Order = 3,
                    Name = "Create wallet",
                    Service = "RockBreaker.Pay.Api",
                    Method = "POST",
                    Endpoint = "/api/wallets",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Bearer(),
                    ExpectedSuccess = "200 OK; data.id SourceWalletId olarak kullanılır ve ilk bakiye 0 TRY olur.",
                    Integrations = ["JWT", "Wallet Repository", "Audit"],
                    Architecture = "Wallet Application Service + Repository"
                },
                new CustomerJourneyStep
                {
                    Order = 4,
                    Name = "Open fake bank account",
                    Service = "RockBreaker.FakeBanking.Api",
                    Method = "POST",
                    Endpoint = "http://localhost:5101/api/banking/accounts",
                    Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
                    RequestBody = new { customerName = "Customer One", initialBalance = 10000.00m },
                    ExpectedSuccess = "200 OK; response id değeri BankAccountId olur.",
                    Integrations = ["FakeBanking"],
                    Architecture = "Fake External Service"
                },
                new CustomerJourneyStep
                {
                    Order = 5,
                    Name = "Fund wallet from bank",
                    Service = "RockBreaker.Pay.Api -> FakeCutoff -> Fraud -> FakeBanking",
                    Method = "POST",
                    Endpoint = "/api/bank-transfers/bank-to-wallet",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Financial("bank-to-wallet-transfer-001"),
                    RequestBody = new { bankAccountId = "<bank-account-id-from-step-4>", amount = 1000.00m },
                    ExpectedSuccess = "200 OK; wallet bakiyesi artar.",
                    Integrations = ["JWT", "KYC", "FakeCutoff", "Fraud Engine", "FakeBanking", "Idempotency", "Ledger", "Saga Compensation", "Audit"],
                    Architecture = "Saga Orchestration + Compensation"
                },
                new CustomerJourneyStep
                {
                    Order = 6,
                    Name = "Transfer to verified destination wallet",
                    Service = "RockBreaker.Pay.Api",
                    Method = "POST",
                    Endpoint = "/api/wallet/transfers",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Financial("wallet-transfer-001"),
                    RequestBody = new { sourceWalletId = "<wallet-id-from-step-3>", destinationWalletId = "<verified-active-destination-wallet-id>", amount = 250.00m, currency = "TRY" },
                    ExpectedSuccess = "200 OK; transfer Completed olur ve iki wallet bakiyesi atomik olarak güncellenir.",
                    Integrations = ["JWT Ownership", "Source KYC", "Destination KYC", "Fraud Engine", "Wallet Limits", "Idempotency", "Pessimistic Locking", "Double-Entry Ledger", "Transactional Outbox", "FakeNotification", "Audit"],
                    Architecture = "Application Service + Unit of Work + Double-Entry Ledger + Transactional Outbox"
                }
            ]
        });
    }
}

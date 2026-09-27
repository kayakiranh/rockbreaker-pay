using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.CustomerJourney;

/// <summary>
/// TR: Kampanya katılımı sonrası kampanyaya bağlı transfer success akışını dokümante eder.
/// EN: Documents the successful campaign participation and campaign-transfer flow.
/// Architecture: Read-Only API Journey Controller.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/customer-journey/campaign-transfer")]
public sealed class CampaignTransferJourneyController : ControllerBase
{
    /// <summary>TR: Kampanya transfer journey'sini success request örnekleriyle döndürür. EN: Returns the campaign-transfer journey with successful request examples. Architecture: Documentation Query.</summary>
    [HttpGet]
    public ActionResult<CustomerJourneyResponse> Get()
    {
        return Ok(new CustomerJourneyResponse
        {
            Title = "Register -> KYC -> Fund Wallet -> Join Campaign -> Campaign Transfer",
            Notes =
            [
                "Kampanyaya katılım için kullanıcı KYC Verified olmalıdır.",
                "Campaign transfer önce FakeCampaign katılımını ve minimum tutarı doğrular, sonra core WalletTransferService'i çağırır.",
                "Bu nedenle kampanya transferi normal transferin Fraud, KYC, limit, locking, ledger ve notification korumalarını atlayamaz."
            ],
            Steps =
            [
                new CustomerJourneyStep
                {
                    Order = 1,
                    Name = "Prepare verified funded wallet",
                    Service = "RockBreaker.Pay.Api -> FakeKyc -> FakeBanking",
                    Method = "POST",
                    Endpoint = "/api/auth/register -> /api/kyc/submit -> /api/wallets -> /api/bank-transfers/bank-to-wallet",
                    RequestBody = new { fundingAmount = 2000.00m },
                    ExpectedSuccess = "Verified customer with active funded wallet.",
                    Integrations = ["Identity", "KYC", "FakeCutoff", "Fraud Engine", "FakeBanking", "Ledger", "Audit"],
                    Architecture = "Composed Customer Onboarding Flow"
                },
                new CustomerJourneyStep
                {
                    Order = 2,
                    Name = "List campaigns",
                    Service = "RockBreaker.Pay.Api -> FakeCampaign",
                    Method = "GET",
                    Endpoint = "/api/campaigns",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Bearer(),
                    ExpectedSuccess = "200 OK; seçilen kampanyanın id, minimumAmount ve cashbackAmount bilgileri alınır.",
                    Integrations = ["JWT", "FakeCampaign", "Anti-Corruption Layer", "Audit"],
                    Architecture = "External Query Adapter"
                },
                new CustomerJourneyStep
                {
                    Order = 3,
                    Name = "Join campaign",
                    Service = "RockBreaker.Pay.Api -> FakeCampaign",
                    Method = "POST",
                    Endpoint = "/api/campaigns/<campaign-id>/join",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Bearer(),
                    ExpectedSuccess = "200 OK; IsJoined=true.",
                    Integrations = ["JWT", "KYC", "FakeCampaign Participation", "Audit"],
                    Architecture = "Application Command + External Participation Store"
                },
                new CustomerJourneyStep
                {
                    Order = 4,
                    Name = "Make campaign transfer",
                    Service = "RockBreaker.Pay.Api -> FakeCampaign -> Core Wallet Transfer",
                    Method = "POST",
                    Endpoint = "/api/campaigns/<campaign-id>/transfer",
                    RequiresBearerToken = true,
                    Headers = JourneyHeaders.Financial("campaign-transfer-001"),
                    RequestBody = new { sourceWalletId = "<customer-wallet-id>", destinationWalletId = "<verified-active-destination-wallet-id>", amount = 1000.00m, currency = "TRY" },
                    ExpectedSuccess = "200 OK; kampanya katılımı ve minimum tutar doğrulanır, transfer Completed olur.",
                    Integrations = ["JWT Ownership", "Campaign Participation", "Campaign Minimum Amount", "Source KYC", "Destination KYC", "Fraud Engine", "Wallet Limits", "Idempotency", "Pessimistic Locking", "Double-Entry Ledger", "Transactional Outbox", "FakeNotification", "Audit"],
                    Architecture = "Use-Case Composition over Secure Core Transfer"
                }
            ]
        });
    }
}

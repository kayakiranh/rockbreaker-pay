using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.CustomerJourney;

/// <summary>
/// TR: Bir müşterinin register işleminden başarılı wallet transferine kadar geçeceği endpoint akışını ve örnek success request'lerini döndürür.
/// EN: Returns the endpoint journey and example successful requests from customer registration to a successful wallet transfer.
/// Architecture: Developer Experience / API Journey Documentation Controller. İş mantığı çalıştırmaz; yalnız mevcut API sözleşmelerini dokümante eder.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/customer-journey")]
public sealed class CustomerJourneyController : ControllerBase
{
    /// <summary>
    /// TR: Register, KYC, wallet oluşturma, bankadan para yükleme ve wallet-to-wallet transfer adımlarını sıralı olarak döndürür.
    /// EN: Returns registration, KYC, wallet creation, bank funding and wallet-to-wallet transfer steps in execution order.
    /// Architecture: Read-Only Documentation Endpoint.
    /// </summary>
    /// <returns>TR: Başarılı müşteri akışı ve örnek request'ler. EN: Successful customer journey and example requests.</returns>
    [HttpGet("register-to-transfer")]
    [ProducesResponseType(typeof(CustomerJourneyResponse), StatusCodes.Status200OK)]
    public ActionResult<CustomerJourneyResponse> GetRegisterToTransferJourney()
    {
        return Ok(new CustomerJourneyResponse
        {
            Title = "Customer register-to-transfer success journey",
            Notes =
            [
                "Register endpoint accessToken ve refreshToken döndürür; ilk akışta ayrıca login çağrısı zorunlu değildir.",
                "KYC submit başarılı transferlerden önce Verified olmalıdır. Normal FakeKyc senaryosu Verified döndürür.",
                "Yeni wallet 0 TRY bakiye ile açılır. Transfer yapabilmek için önce wallet'a para yüklenmelidir.",
                "Bank-to-wallet işlemi FakeCutoff çalışma saati/politikasına tabidir.",
                "Financial POST endpoint'lerinde her yeni business işlem için benzersiz Idempotency-Key kullanılmalıdır.",
                "Son wallet transferindeki DestinationWalletId başka bir aktif ve KYC doğrulanmış müşterinin wallet kimliği olmalıdır."
            ],
            Steps =
            [
                CreateRegisterStep(),
                CreateKycStep(),
                CreateWalletStep(),
                CreateFakeBankAccountStep(),
                CreateBankToWalletStep(),
                CreateWalletSummaryStep(),
                CreateWalletTransferStep(),
                CreateOptionalLoginStep()
            ]
        });
    }

    /// <summary>
    /// TR: Register endpoint'i için başarılı örnek adımı oluşturur.
    /// EN: Creates the successful example step for the registration endpoint.
    /// Architecture: Explicit Documentation Factory.
    /// </summary>
    private static CustomerJourneyStep CreateRegisterStep() => new()
    {
        Order = 1,
        Name = "Register customer",
        Service = "RockBreaker.Pay.Api",
        Method = "POST",
        Endpoint = "/api/auth/register",
        RequiresBearerToken = false,
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        },
        RequestBody = new
        {
            email = "customer@rockbreaker.local",
            password = "Customer123!",
            firstName = "Customer",
            lastName = "One"
        },
        ExpectedSuccess = "200 OK. Response içindeki data.accessToken sonraki korumalı endpoint'lerde Bearer token olarak kullanılır.",
        Architecture = "Identity Application Service + PBKDF2 Password Hashing + JWT/Refresh Token"
    };

    /// <summary>
    /// TR: KYC doğrulama endpoint'i için başarılı örnek adımı oluşturur.
    /// EN: Creates the successful example step for the KYC verification endpoint.
    /// Architecture: Explicit Documentation Factory.
    /// </summary>
    private static CustomerJourneyStep CreateKycStep() => new()
    {
        Order = 2,
        Name = "Submit KYC verification",
        Service = "RockBreaker.Pay.Api -> RockBreaker.FakeKyc.Api",
        Method = "POST",
        Endpoint = "/api/kyc/submit",
        RequiresBearerToken = true,
        Headers = BearerHeaders(),
        RequestBody = null,
        ExpectedSuccess = "200 OK ve status=Verified. Ana API FakeKyc /api/kyc/verifications endpoint'ini çağırır.",
        Architecture = "Compliance Application Service + Anti-Corruption Layer + Fake External KYC Provider"
    };

    /// <summary>
    /// TR: Wallet oluşturma endpoint'i için başarılı örnek adımı oluşturur.
    /// EN: Creates the successful example step for the wallet-creation endpoint.
    /// Architecture: Explicit Documentation Factory.
    /// </summary>
    private static CustomerJourneyStep CreateWalletStep() => new()
    {
        Order = 3,
        Name = "Create customer wallet",
        Service = "RockBreaker.Pay.Api",
        Method = "POST",
        Endpoint = "/api/wallets",
        RequiresBearerToken = true,
        Headers = BearerHeaders(),
        RequestBody = null,
        ExpectedSuccess = "200 OK. Response içindeki data.id değeri SourceWalletId olarak saklanır. İlk bakiye 0 TRY'dir.",
        Architecture = "Wallet Application Service + Repository Pattern"
    };

    /// <summary>
    /// TR: Fake banka hesabı oluşturma endpoint'i için başarılı örnek adımı oluşturur.
    /// EN: Creates the successful example step for opening a fake bank account.
    /// Architecture: Explicit Documentation Factory.
    /// </summary>
    private static CustomerJourneyStep CreateFakeBankAccountStep() => new()
    {
        Order = 4,
        Name = "Open fake bank account",
        Service = "RockBreaker.FakeBanking.Api",
        Method = "POST",
        Endpoint = "http://localhost:5101/api/banking/accounts",
        RequiresBearerToken = false,
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        },
        RequestBody = new
        {
            customerName = "Customer One",
            initialBalance = 10000.00m
        },
        ExpectedSuccess = "200 OK. Response içindeki id değeri BankAccountId olarak sonraki adımda kullanılır.",
        Architecture = "Fake External Service + In-Memory Repository"
    };

    /// <summary>
    /// TR: Bankadan wallet'a bakiye yükleme endpoint'i için başarılı örnek adımı oluşturur.
    /// EN: Creates the successful example step for funding the wallet from a bank account.
    /// Architecture: Explicit Documentation Factory.
    /// </summary>
    private static CustomerJourneyStep CreateBankToWalletStep()
    {
        var headers = BearerHeaders();
        headers["Idempotency-Key"] = "bank-to-wallet-customer-001";

        return new CustomerJourneyStep
        {
            Order = 5,
            Name = "Fund wallet from bank",
            Service = "RockBreaker.Pay.Api -> FakeCutoff + Fraud + FakeBanking",
            Method = "POST",
            Endpoint = "/api/bank-transfers/bank-to-wallet",
            RequiresBearerToken = true,
            Headers = headers,
            RequestBody = new
            {
                bankAccountId = "<bank-account-id-from-step-4>",
                amount = 1000.00m
            },
            ExpectedSuccess = "200 OK. Wallet bakiyesi 1000 TRY artar. İşlem cutoff saatleri içinde ve fraud kurallarına uygun olmalıdır.",
            Architecture = "Saga Orchestration + Cutoff Policy + Fraud Evaluation + Idempotency + Ledger"
        };
    }

    /// <summary>
    /// TR: Transfer öncesi wallet özetini kontrol eden başarılı örnek adımı oluşturur.
    /// EN: Creates the successful example step for checking wallet summary before transfer.
    /// Architecture: Explicit Documentation Factory.
    /// </summary>
    private static CustomerJourneyStep CreateWalletSummaryStep() => new()
    {
        Order = 6,
        Name = "Check wallet balance",
        Service = "RockBreaker.Pay.Api",
        Method = "GET",
        Endpoint = "/api/wallets/me",
        RequiresBearerToken = true,
        Headers = BearerHeaders(),
        RequestBody = null,
        ExpectedSuccess = "200 OK. data.balance transfer tutarından büyük veya eşit olmalıdır.",
        Architecture = "Wallet Query + Repository Pattern"
    };

    /// <summary>
    /// TR: Wallet-to-wallet transfer endpoint'i için başarılı örnek adımı oluşturur.
    /// EN: Creates the successful example step for the wallet-to-wallet transfer endpoint.
    /// Architecture: Explicit Documentation Factory.
    /// </summary>
    private static CustomerJourneyStep CreateWalletTransferStep()
    {
        var headers = BearerHeaders();
        headers["Idempotency-Key"] = "wallet-transfer-customer-001";

        return new CustomerJourneyStep
        {
            Order = 7,
            Name = "Transfer money to another wallet",
            Service = "RockBreaker.Pay.Api",
            Method = "POST",
            Endpoint = "/api/wallet/transfers",
            RequiresBearerToken = true,
            Headers = headers,
            RequestBody = new
            {
                sourceWalletId = "<wallet-id-from-step-3>",
                destinationWalletId = "<another-active-verified-wallet-id>",
                amount = 250.00m,
                currency = "TRY"
            },
            ExpectedSuccess = "200 OK. Transfer Completed olur; source/debit ve destination/credit ledger kayıtları aynı SQL transaction içinde oluşturulur.",
            Architecture = "Ownership Authorization + KYC Guard + Fraud Rule Engine + Idempotency + Pessimistic Locking + Double-Entry Ledger + Transactional Outbox"
        };
    }

    /// <summary>
    /// TR: Register sonrası sonraki oturumlarda kullanılabilecek opsiyonel login adımını oluşturur.
    /// EN: Creates the optional login step used for later sessions after registration.
    /// Architecture: Explicit Documentation Factory.
    /// </summary>
    private static CustomerJourneyStep CreateOptionalLoginStep() => new()
    {
        Order = 8,
        Name = "Optional login for a later session",
        Service = "RockBreaker.Pay.Api",
        Method = "POST",
        Endpoint = "/api/auth/login",
        RequiresBearerToken = false,
        IsOptional = true,
        Headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        },
        RequestBody = new
        {
            email = "customer@rockbreaker.local",
            password = "Customer123!"
        },
        ExpectedSuccess = "200 OK. Yeni accessToken ve refreshToken çifti döner.",
        Architecture = "Identity Application Service + JWT Authentication"
    };

    /// <summary>
    /// TR: Bearer token gerektiren örnek endpoint'ler için standart header sözlüğü oluşturur.
    /// EN: Creates the standard header dictionary for example endpoints requiring a bearer token.
    /// Architecture: Documentation Helper.
    /// </summary>
    private static Dictionary<string, string> BearerHeaders() => new()
    {
        ["Authorization"] = "Bearer <access-token-from-register-or-login>",
        ["Content-Type"] = "application/json"
    };
}

/// <summary>
/// TR: Register'dan transfere kadar müşteri journey dokümanını temsil eder.
/// EN: Represents the customer-journey document from registration to transfer.
/// Architecture: Documentation Response DTO.
/// </summary>
public sealed class CustomerJourneyResponse
{
    /// <summary>TR: Journey başlığı. EN: Journey title. Architecture: DTO Property.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>TR: Akışın önemli başarı koşulları ve kullanım notları. EN: Important success conditions and usage notes for the flow. Architecture: DTO Property.</summary>
    public IReadOnlyCollection<string> Notes { get; init; } = Array.Empty<string>();

    /// <summary>TR: Sıralı endpoint adımları. EN: Ordered endpoint steps. Architecture: DTO Property.</summary>
    public IReadOnlyCollection<CustomerJourneyStep> Steps { get; init; } = Array.Empty<CustomerJourneyStep>();
}

/// <summary>
/// TR: Müşteri journey'sindeki tek endpoint adımını ve success request örneğini temsil eder.
/// EN: Represents one endpoint step and its successful request example in the customer journey.
/// Architecture: Documentation Response DTO.
/// </summary>
public sealed class CustomerJourneyStep
{
    /// <summary>TR: Çalıştırma sırası. EN: Execution order. Architecture: DTO Property.</summary>
    public int Order { get; init; }

    /// <summary>TR: Adımın açıklayıcı adı. EN: Descriptive step name. Architecture: DTO Property.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>TR: Çağrının yapıldığı servis veya entegrasyon zinciri. EN: Service or integration chain receiving the request. Architecture: DTO Property.</summary>
    public string Service { get; init; } = string.Empty;

    /// <summary>TR: HTTP metodu. EN: HTTP method. Architecture: DTO Property.</summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>TR: Endpoint adresi. EN: Endpoint address. Architecture: DTO Property.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>TR: Bearer token gerekip gerekmediği. EN: Whether a bearer token is required. Architecture: Security Metadata.</summary>
    public bool RequiresBearerToken { get; init; }

    /// <summary>TR: Adımın zorunlu olmayıp olmadığını belirtir. EN: Indicates whether the step is optional. Architecture: Journey Metadata.</summary>
    public bool IsOptional { get; init; }

    /// <summary>TR: Örnek request header'ları. EN: Example request headers. Architecture: Documentation Property.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();

    /// <summary>TR: Örnek başarılı request body; body gerekmiyorsa null. EN: Example successful request body; null when no body is required. Architecture: Documentation Property.</summary>
    public object? RequestBody { get; init; }

    /// <summary>TR: Beklenen başarılı sonuç ve bir sonraki adımda kullanılacak veri. EN: Expected successful result and data used by the next step. Architecture: Journey Metadata.</summary>
    public string ExpectedSuccess { get; init; } = string.Empty;

    /// <summary>TR: Endpoint'in arkasındaki temel mimari/pattern bilgisi. EN: Core architecture/pattern information behind the endpoint. Architecture: Documentation Metadata.</summary>
    public string Architecture { get; init; } = string.Empty;
}

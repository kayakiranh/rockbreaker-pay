using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.FakeBanking.Controllers;

/// <summary>
/// TR: Banka hesabı açma, wallet yönlü transfer ve banka hareketlerini simüle eder.
/// EN: Simulates bank account creation, wallet-facing transfers and bank movements.
/// Architecture: Fake External Service + In-Memory Repository.
/// </summary>
[ApiController]
[Route("api/banking")]
public sealed class BankingController : ControllerBase
{
    private static readonly ConcurrentDictionary<Guid, BankAccount> Accounts = new();

    /// <summary>
    /// TR: Yeni bir fake banka hesabı açar.
    /// EN: Opens a new fake bank account.
    /// Architecture: REST Command.
    /// </summary>
    /// <param name="request">TR: Hesap açma isteği. EN: Account opening request.</param>
    /// <returns>TR: Oluşturulan hesap. EN: Created account.</returns>
    [HttpPost("accounts")]
    public ActionResult<BankAccount> OpenAccount([FromBody] OpenBankAccountRequest request)
    {
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            Iban = $"TR{Random.Shared.NextInt64(100000000000000000, 999999999999999999)}",
            Balance = request.InitialBalance
        };

        Accounts[account.Id] = account;
        return Ok(account);
    }

    /// <summary>
    /// TR: Banka hesabından wallet'a para çıkışını simüle eder ve banka hareketine işler.
    /// EN: Simulates a transfer from a bank account to a wallet and records the bank movement.
    /// Architecture: REST Command + Fake Ledger.
    /// </summary>
    /// <param name="request">TR: Transfer isteği. EN: Transfer request.</param>
    /// <returns>TR: Oluşturulan banka hareketi. EN: Created bank movement.</returns>
    [HttpPost("transfers/to-wallet")]
    public ActionResult<BankMovement> TransferToWallet([FromBody] BankWalletTransferRequest request)
    {
        if (!Accounts.TryGetValue(request.BankAccountId, out var account))
        {
            return NotFound();
        }

        if (account.Balance < request.Amount)
        {
            return BadRequest("Insufficient bank balance.");
        }

        account.Balance -= request.Amount;
        var movement = account.AddMovement("BankToWallet", -request.Amount, request.WalletId);
        return Ok(movement);
    }

    /// <summary>
    /// TR: Wallet'tan banka hesabına para girişini simüle eder ve banka hareketine işler.
    /// EN: Simulates a transfer from a wallet to a bank account and records the bank movement.
    /// Architecture: REST Command + Fake Ledger.
    /// </summary>
    /// <param name="request">TR: Transfer isteği. EN: Transfer request.</param>
    /// <returns>TR: Oluşturulan banka hareketi. EN: Created bank movement.</returns>
    [HttpPost("transfers/from-wallet")]
    public ActionResult<BankMovement> TransferFromWallet([FromBody] BankWalletTransferRequest request)
    {
        if (!Accounts.TryGetValue(request.BankAccountId, out var account))
        {
            return NotFound();
        }

        account.Balance += request.Amount;
        var movement = account.AddMovement("WalletToBank", request.Amount, request.WalletId);
        return Ok(movement);
    }

    /// <summary>
    /// TR: Banka hesabının tüm hareketlerini döndürür.
    /// EN: Returns all movements of a bank account.
    /// Architecture: REST Query.
    /// </summary>
    /// <param name="request">TR: Telafi edilecek banka hareketinin bilgileri. EN: Details of the bank movement to compensate.</param>
    /// <returns>TR: Oluşturulan ters banka hareketi. EN: Created reversing bank movement.</returns>
    [HttpPost("transfers/reverse")]
    public ActionResult<BankMovement> Reverse([FromBody] ReverseBankTransferRequest request)
    {
        if (!Accounts.TryGetValue(request.BankAccountId, out var account))
        {
            return NotFound();
        }

        if (string.Equals(request.OriginalType, "BankToWallet", StringComparison.OrdinalIgnoreCase))
        {
            account.Balance += request.Amount;
            return Ok(account.AddMovement("BankToWalletReversal", request.Amount, request.WalletId));
        }

        if (account.Balance < request.Amount)
        {
            return BadRequest("Insufficient bank balance for reversal.");
        }

        account.Balance -= request.Amount;
        return Ok(account.AddMovement("WalletToBankReversal", -request.Amount, request.WalletId));
    }

    /// <summary>
    /// TR: Banka hesabının tüm hareketlerini döndürür.
    /// EN: Returns all movements of a bank account.
    /// Architecture: REST Query.
    /// </summary>
    [HttpGet("accounts/{accountId:guid}/movements")]
    public ActionResult<IReadOnlyCollection<BankMovement>> GetMovements(Guid accountId)
    {
        return Accounts.TryGetValue(accountId, out var account)
            ? Ok(account.Movements)
            : NotFound();
    }
}

/// <summary>
/// TR: Fake banka telafi isteğidir.
/// EN: Fake bank compensation request.
/// Architecture: Saga Compensation DTO.
/// </summary>
public sealed class ReverseBankTransferRequest
{
    /// <summary>TR: Banka hesap kimliği. EN: Bank account identifier. Architecture: DTO Property.</summary>
    public Guid BankAccountId { get; init; }
    /// <summary>TR: Wallet kimliği. EN: Wallet identifier. Architecture: DTO Property.</summary>
    public Guid WalletId { get; init; }
    /// <summary>TR: Tutar. EN: Amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: Orijinal işlem tipi. EN: Original transaction type. Architecture: Compensation Metadata.</summary>
    public string OriginalType { get; init; } = string.Empty;
}

/// <summary>
/// TR: Fake banka hesabı açma isteğidir.
/// EN: Fake bank account opening request.
/// Architecture: Request DTO.
/// </summary>
public sealed class OpenBankAccountRequest
{
    /// <summary>TR: Müşteri adı. EN: Customer name. Architecture: DTO Property.</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>TR: Başlangıç bakiyesi. EN: Initial balance. Architecture: DTO Property.</summary>
    public decimal InitialBalance { get; init; }
}

/// <summary>
/// TR: Banka ile wallet arasındaki fake transfer isteğidir.
/// EN: Fake transfer request between bank and wallet.
/// Architecture: Request DTO.
/// </summary>
public sealed class BankWalletTransferRequest
{
    /// <summary>TR: Banka hesap kimliği. EN: Bank account identifier. Architecture: DTO Property.</summary>
    public Guid BankAccountId { get; init; }

    /// <summary>TR: Wallet kimliği. EN: Wallet identifier. Architecture: DTO Property.</summary>
    public Guid WalletId { get; init; }

    /// <summary>TR: Transfer tutarı. EN: Transfer amount. Architecture: DTO Property.</summary>
    public decimal Amount { get; init; }
}

/// <summary>
/// TR: Fake banka hesabını temsil eder.
/// EN: Represents a fake bank account.
/// Architecture: In-Memory Entity.
/// </summary>
public sealed class BankAccount
{
    private readonly List<BankMovement> _movements = [];

    /// <summary>TR: Hesap kimliği. EN: Account identifier. Architecture: Entity Identity.</summary>
    public Guid Id { get; init; }

    /// <summary>TR: Müşteri adı. EN: Customer name. Architecture: Entity Property.</summary>
    public string CustomerName { get; init; } = string.Empty;

    /// <summary>TR: Fake IBAN. EN: Fake IBAN. Architecture: External Identifier.</summary>
    public string Iban { get; init; } = string.Empty;

    /// <summary>TR: Banka bakiyesi. EN: Bank balance. Architecture: Entity State.</summary>
    public decimal Balance { get; set; }

    /// <summary>TR: Hesap hareketleri. EN: Account movements. Architecture: Read-Only Collection.</summary>
    public IReadOnlyCollection<BankMovement> Movements => _movements.AsReadOnly();

    /// <summary>
    /// TR: Hesaba yeni fake hareket ekler.
    /// EN: Adds a new fake movement to the account.
    /// Architecture: Encapsulated Entity Behavior.
    /// </summary>
    /// <param name="type">TR: Hareket tipi. EN: Movement type.</param>
    /// <param name="amount">TR: Hareket tutarı. EN: Movement amount.</param>
    /// <param name="walletId">TR: İlgili wallet. EN: Related wallet.</param>
    /// <returns>TR: Yeni hareket. EN: New movement.</returns>
    public BankMovement AddMovement(string type, decimal amount, Guid walletId)
    {
        var movement = new BankMovement
        {
            Id = Guid.NewGuid(),
            Type = type,
            Amount = amount,
            WalletId = walletId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _movements.Add(movement);
        return movement;
    }
}

/// <summary>
/// TR: Fake banka hareketini temsil eder.
/// EN: Represents a fake bank movement.
/// Architecture: Fake Ledger Entry.
/// </summary>
public sealed class BankMovement
{
    /// <summary>TR: Hareket kimliği. EN: Movement identifier. Architecture: Entity Identity.</summary>
    public Guid Id { get; init; }
    /// <summary>TR: Hareket tipi. EN: Movement type. Architecture: Business Classification.</summary>
    public string Type { get; init; } = string.Empty;
    /// <summary>TR: Tutar. EN: Amount. Architecture: Monetary Value.</summary>
    public decimal Amount { get; init; }
    /// <summary>TR: İlgili wallet kimliği. EN: Related wallet identifier. Architecture: External Reference.</summary>
    public Guid WalletId { get; init; }
    /// <summary>TR: UTC kayıt zamanı. EN: UTC creation time. Architecture: Audit Metadata.</summary>
    public DateTime CreatedAtUtc { get; init; }
}

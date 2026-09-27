using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Banking;

/// <summary>
/// TR: Banka-wallet para transfer endpoint'lerini sunar.
/// EN: Exposes bank-wallet money transfer endpoints.
/// Architecture: Thin REST Controller.
/// </summary>
[ApiController]
[Authorize]
[Route("api/bank-transfers")]
public sealed class BankTransfersController : ControllerBase
{
    private readonly IBankTransferService _service;

    /// <summary>TR: Controller bağımlılıklarını alır. EN: Receives controller dependencies. Architecture: Constructor Injection.</summary>
    public BankTransfersController(IBankTransferService service) => _service = service;

    /// <summary>TR: Bankadan wallet'a para aktarır. EN: Transfers money from bank to wallet. Architecture: Saga REST Command.</summary>
    [HttpPost("bank-to-wallet")]
    public async Task<IActionResult> BankToWallet(
        [FromBody] BankTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var result = await _service.BankToWalletAsync(
            GetUserId(),
            request,
            idempotencyKey ?? string.Empty,
            HttpContext.TraceIdentifier);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>TR: Wallet'tan bankaya para aktarır. EN: Transfers money from wallet to bank. Architecture: Saga REST Command.</summary>
    [HttpPost("wallet-to-bank")]
    public async Task<IActionResult> WalletToBank(
        [FromBody] BankTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var result = await _service.WalletToBankAsync(
            GetUserId(),
            request,
            idempotencyKey ?? string.Empty,
            HttpContext.TraceIdentifier);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user id claim is missing."));
}

using Microsoft.AspNetCore.Mvc;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.Wallet.Controllers;

/// <summary>
/// TR: Wallet kullanıcıları arasındaki para transfer endpoint'lerini sunar.
/// EN: Exposes money-transfer endpoints between wallet users.
/// Architecture: Thin Controller; business orchestration stays in Application Service.
/// </summary>
[ApiController]
[Route("api/wallet/transfers")]
public sealed class WalletTransfersController : ControllerBase
{
    private readonly IWalletTransferService _service;

    /// <summary>
    /// TR: Controller bağımlılıklarını alır.
    /// EN: Receives controller dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="service">TR: Wallet transfer application service. EN: Wallet transfer application service.</param>
    public WalletTransfersController(IWalletTransferService service) => _service = service;

    /// <summary>
    /// TR: Bir wallet'tan başka bir wallet'a para transfer eder.
    /// EN: Transfers money from one wallet to another.
    /// Architecture: REST Controller + Idempotent Command.
    /// </summary>
    /// <param name="request">TR: Transfer bilgileri. EN: Transfer details.</param>
    /// <param name="idempotencyKey">TR: Aynı isteğin iki kez uygulanmasını engelleyen header. EN: Header preventing duplicate execution.</param>
    /// <returns>TR: Standart API cevabı. EN: Standard API response.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OperationResult<TransferResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OperationResult<TransferResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TransferAsync(
        [FromBody] TransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var correlationId = HttpContext.TraceIdentifier;
        var result = await _service.TransferAsync(request, idempotencyKey ?? string.Empty, correlationId);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Government;

/// <summary>
/// TR: Devlet SOAP servisi üzerinden fatura görüntüleme ve ödeme endpoint'lerini sunar.
/// EN: Exposes bill viewing and payment endpoints through the government SOAP service.
/// Architecture: Thin REST Controller + SOAP Anti-Corruption Layer.
/// </summary>
[ApiController]
[Authorize]
[Route("api/bills")]
public sealed class BillsController : ControllerBase
{
    private readonly IBillPaymentService _service;

    /// <summary>
    /// TR: Controller bağımlılıklarını alır.
    /// EN: Receives controller dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public BillsController(IBillPaymentService service) => _service = service;

    /// <summary>
    /// TR: Vatandaş numarasına ait açık faturaları listeler.
    /// EN: Lists open bills for a citizen number.
    /// Architecture: REST Query backed by SOAP integration.
    /// </summary>
    /// <param name="citizenNumber">TR: Dummy vatandaş numarası. EN: Dummy citizen number.</param>
    [HttpGet]
    public async Task<IActionResult> GetBills([FromQuery] string citizenNumber) =>
        Ok(await _service.GetBillsAsync(citizenNumber));

    /// <summary>
    /// TR: Faturayı aktif kullanıcının wallet bakiyesinden öder.
    /// EN: Pays the bill from the current user's wallet balance.
    /// Architecture: Saga REST Command.
    /// </summary>
    /// <param name="billId">TR: Fatura kimliği. EN: Bill identifier.</param>
    /// <param name="citizenNumber">TR: Dummy vatandaş numarası. EN: Dummy citizen number.</param>
    /// <param name="idempotencyKey">TR: Tekrarlı ödemeyi engelleyen idempotency anahtarı. EN: Idempotency key preventing duplicate payment.</param>
    [HttpPost("{billId:guid}/pay")]
    public async Task<IActionResult> Pay(
        Guid billId,
        [FromQuery] string citizenNumber,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var result = await _service.PayAsync(
            GetUserId(),
            citizenNumber,
            billId,
            idempotencyKey ?? string.Empty,
            HttpContext.TraceIdentifier);

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// TR: JWT claim içinden aktif kullanıcı kimliğini döndürür.
    /// EN: Returns the current user identifier from the JWT claim.
    /// Architecture: Claims-Based Identity Helper.
    /// </summary>
    /// <returns>TR: Kullanıcı kimliği. EN: User identifier.</returns>
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated user id claim is missing."));
}

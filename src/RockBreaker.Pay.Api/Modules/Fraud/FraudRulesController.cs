using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Fraud kurallarının görüntülenmesi ve yönetilmesi için ayrı controller sunar.
/// EN: Provides a dedicated controller for viewing and managing fraud rules.
/// Architecture: Role-Protected Thin REST Controller.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Auditor")]
[Route("api/fraud/rules")]
public sealed class FraudRulesController : ControllerBase
{
    private readonly IFraudRuleService _service;

    /// <summary>
    /// TR: Controller bağımlılıklarını alır.
    /// EN: Receives controller dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="service">TR: Fraud rule application service. EN: Fraud-rule application service.</param>
    public FraudRulesController(IFraudRuleService service) => _service = service;

    /// <summary>
    /// TR: Tüm fraud kurallarını listeler.
    /// EN: Lists all fraud rules.
    /// Architecture: REST Query.
    /// </summary>
    /// <returns>TR: Fraud kuralları. EN: Fraud rules.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<FraudRuleResponse>>> GetAsync() =>
        Ok(await _service.GetRulesAsync());

    /// <summary>
    /// TR: Fraud kuralını günceller; yalnız Admin rolü kullanabilir.
    /// EN: Updates a fraud rule; only the Admin role may execute this operation.
    /// Architecture: REST Command + Role-Based Authorization.
    /// </summary>
    /// <param name="ruleId">TR: Fraud kural kimliği. EN: Fraud-rule identifier.</param>
    /// <param name="request">TR: Yeni kural değerleri. EN: New rule values.</param>
    /// <returns>TR: Güncel kural veya hata. EN: Updated rule or error.</returns>
    [HttpPut("{ruleId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAsync(
        int ruleId,
        [FromBody] UpdateFraudRuleRequest request)
    {
        var result = await _service.UpdateAsync(ruleId, request);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}

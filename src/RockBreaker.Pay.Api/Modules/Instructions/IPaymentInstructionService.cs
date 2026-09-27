using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Otomatik talimat yönetim use-case'lerini tanımlar.
/// EN: Defines automatic instruction management use cases.
/// Architecture: Application Service Interface.
/// </summary>
public interface IPaymentInstructionService
{
    /// <summary>TR: Yeni talimat oluşturur. EN: Creates a new instruction. Architecture: Application Command.</summary>
    Task<OperationResult<PaymentInstructionResponse>> CreateAsync(Guid userId, CreatePaymentInstructionRequest request);

    /// <summary>TR: Kullanıcının talimatlarını listeler. EN: Lists the user's instructions. Architecture: Application Query.</summary>
    Task<OperationResult<IReadOnlyCollection<PaymentInstructionResponse>>> GetMineAsync(Guid userId);

    /// <summary>TR: Talimatı duraklatır. EN: Pauses an instruction. Architecture: Application Command.</summary>
    Task<OperationResult<PaymentInstructionResponse>> PauseAsync(Guid userId, Guid instructionId);

    /// <summary>TR: Duraklatılmış/başarısız talimatı yeniden aktif eder. EN: Reactivates a paused/failed instruction. Architecture: Application Command.</summary>
    Task<OperationResult<PaymentInstructionResponse>> ResumeAsync(Guid userId, Guid instructionId);

    /// <summary>TR: Talimatı iptal eder. EN: Cancels an instruction. Architecture: Application Command.</summary>
    Task<OperationResult<PaymentInstructionResponse>> CancelAsync(Guid userId, Guid instructionId);
}

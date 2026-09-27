using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.Government;

/// <summary>
/// TR: Fatura listeleme ve wallet'tan ödeme use-case'lerini tanımlar.
/// EN: Defines bill listing and wallet-payment use cases.
/// Architecture: Application Service Interface.
/// </summary>
public interface IBillPaymentService
{
    /// <summary>TR: Vatandaş faturalarını listeler. EN: Lists citizen bills. Architecture: Application Query.</summary>
    Task<OperationResult<IReadOnlyCollection<BillResponse>>> GetBillsAsync(string citizenNumber);

    /// <summary>TR: Faturayı wallet bakiyesinden öder. EN: Pays a bill from wallet balance. Architecture: Saga Command.</summary>
    Task<OperationResult<BillPaymentResponse>> PayAsync(
        Guid userId,
        string citizenNumber,
        Guid billId,
        string idempotencyKey,
        string correlationId);
}

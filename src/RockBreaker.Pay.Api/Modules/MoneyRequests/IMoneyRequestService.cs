using RockBreaker.Pay.Common;

namespace RockBreaker.Pay.Modules.MoneyRequests;

/// <summary>
/// TR: Borç/para isteği use-case'lerini tanımlar.
/// EN: Defines money-request use cases.
/// Architecture: Application Service Interface.
/// </summary>
public interface IMoneyRequestService
{
    /// <summary>TR: Yeni para isteği oluşturur. EN: Creates a new money request. Architecture: Application Command.</summary>
    Task<OperationResult<MoneyRequestResponse>> CreateAsync(Guid userId, CreateMoneyRequest request);
    /// <summary>TR: Kullanıcının ilgili para isteklerini listeler. EN: Lists money requests related to the user. Architecture: Application Query.</summary>
    Task<OperationResult<IReadOnlyCollection<MoneyRequestResponse>>> GetMineAsync(Guid userId);
    /// <summary>TR: Para isteğini kabul edip finansal transferi başlatır. EN: Accepts a money request and starts the financial transfer. Architecture: Application Command + Reuse of Transfer Use Case.</summary>
    Task<OperationResult<MoneyRequestResponse>> AcceptAsync(Guid userId, Guid requestId);
    /// <summary>TR: Para isteğini reddeder. EN: Rejects a money request. Architecture: Application Command.</summary>
    Task<OperationResult<MoneyRequestResponse>> RejectAsync(Guid userId, Guid requestId);
    /// <summary>TR: İstek sahibinin para isteğini iptal etmesini sağlar. EN: Allows the requester to cancel the money request. Architecture: Application Command.</summary>
    Task<OperationResult<MoneyRequestResponse>> CancelAsync(Guid userId, Guid requestId);
}

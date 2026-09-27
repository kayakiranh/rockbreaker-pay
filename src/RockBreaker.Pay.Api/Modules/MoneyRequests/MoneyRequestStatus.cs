namespace RockBreaker.Pay.Modules.MoneyRequests;

/// <summary>
/// TR: Borç/para isteğinin yaşam döngüsü durumunu belirtir.
/// EN: Defines the lifecycle status of a money request.
/// Architecture: Explicit State Model.
/// </summary>
public enum MoneyRequestStatus
{
    /// <summary>TR: Cevap bekliyor. EN: Waiting for response. Architecture: State Model.</summary>
    Pending = 1,
    /// <summary>TR: Kabul edildi ve transfer oluşturuldu. EN: Accepted and transfer created. Architecture: State Model.</summary>
    Accepted = 2,
    /// <summary>TR: Alıcı reddetti. EN: Recipient rejected. Architecture: State Model.</summary>
    Rejected = 3,
    /// <summary>TR: İstek sahibi iptal etti. EN: Requester cancelled. Architecture: State Model.</summary>
    Cancelled = 4,
    /// <summary>TR: Süresi doldu. EN: Expired. Architecture: State Model.</summary>
    Expired = 5
}

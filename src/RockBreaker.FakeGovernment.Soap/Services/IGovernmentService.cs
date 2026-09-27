using CoreWCF;

namespace RockBreaker.FakeGovernment.Services;

/// <summary>
/// TR: Fatura listeleme ve ödeme operasyonlarını sunan fake devlet SOAP servis sözleşmesidir.
/// EN: Fake government SOAP service contract exposing bill listing and payment operations.
/// Architecture: SOAP Service Contract + Anti-Corruption Boundary.
/// </summary>
[ServiceContract]
public interface IGovernmentService
{
    /// <summary>
    /// TR: Vatandaş için açık faturaları döndürür.
    /// EN: Returns open bills for a citizen.
    /// Architecture: SOAP Query Operation.
    /// </summary>
    /// <param name="citizenNumber">TR: Dummy vatandaş numarası. EN: Dummy citizen number.</param>
    /// <returns>TR: Faturalar. EN: Bills.</returns>
    [OperationContract]
    BillDto[] GetBills(string citizenNumber);

    /// <summary>
    /// TR: Belirtilen faturayı ödenmiş olarak işaretler.
    /// EN: Marks the specified bill as paid.
    /// Architecture: SOAP Command Operation.
    /// </summary>
    /// <param name="billId">TR: Fatura kimliği. EN: Bill identifier.</param>
    /// <returns>TR: Ödeme sonucu. EN: Payment result.</returns>
    [OperationContract]
    PaymentResultDto PayBill(Guid billId);
}

/// <summary>
/// TR: SOAP üzerinden taşınan fake fatura bilgisidir.
/// EN: Fake bill information transferred over SOAP.
/// Architecture: SOAP Data Contract.
/// </summary>
public sealed class BillDto
{
    /// <summary>TR: Fatura kimliği. EN: Bill identifier. Architecture: Contract Property.</summary>
    public Guid Id { get; set; }
    /// <summary>TR: Kurum adı. EN: Institution name. Architecture: Contract Property.</summary>
    public string Institution { get; set; } = string.Empty;
    /// <summary>TR: Fatura tutarı. EN: Bill amount. Architecture: Contract Property.</summary>
    public decimal Amount { get; set; }
    /// <summary>TR: Ödeme durumu. EN: Payment status. Architecture: Contract Property.</summary>
    public bool IsPaid { get; set; }
}

/// <summary>
/// TR: SOAP fatura ödeme sonucudur.
/// EN: SOAP bill payment result.
/// Architecture: SOAP Data Contract.
/// </summary>
public sealed class PaymentResultDto
{
    /// <summary>TR: İşlemin başarılı olup olmadığı. EN: Whether payment succeeded. Architecture: Contract Property.</summary>
    public bool Success { get; set; }
    /// <summary>TR: Sonuç mesajı. EN: Result message. Architecture: Contract Property.</summary>
    public string Message { get; set; } = string.Empty;
}

namespace RockBreaker.Pay.Modules.Government;

/// <summary>
/// TR: FakeGovernment SOAP servisine erişimi soyutlar.
/// EN: Abstracts access to the FakeGovernment SOAP service.
/// Architecture: Anti-Corruption Layer + Dependency Inversion.
/// </summary>
public interface IGovernmentSoapClient
{
    /// <summary>
    /// TR: Vatandaş numarası için açık faturaları SOAP üzerinden getirir.
    /// EN: Retrieves open bills for a citizen number over SOAP.
    /// Architecture: External SOAP Query Port.
    /// </summary>
    /// <param name="citizenNumber">TR: Dummy vatandaş numarası. EN: Dummy citizen number.</param>
    /// <returns>TR: Açık faturalar. EN: Open bills.</returns>
    Task<IReadOnlyCollection<BillResponse>> GetBillsAsync(string citizenNumber);

    /// <summary>
    /// TR: Faturayı SOAP servisi üzerinden öder.
    /// EN: Pays the bill through the SOAP service.
    /// Architecture: External SOAP Command Port.
    /// </summary>
    /// <param name="billId">TR: Fatura kimliği. EN: Bill identifier.</param>
    /// <returns>TR: Ödeme başarılı ise true. EN: True when payment succeeds.</returns>
    Task<bool> PayBillAsync(Guid billId);
}

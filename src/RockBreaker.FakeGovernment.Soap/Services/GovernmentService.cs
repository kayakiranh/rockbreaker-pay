using System.Collections.Concurrent;

namespace RockBreaker.FakeGovernment.Services;

/// <summary>
/// TR: IGovernmentService sözleşmesinin in-memory fake implementasyonudur.
/// EN: In-memory fake implementation of IGovernmentService.
/// Architecture: Fake Service Adapter.
/// </summary>
public sealed class GovernmentService : IGovernmentService
{
    private static readonly ConcurrentDictionary<Guid, BillDto> Bills = new(
        new[]
        {
            new KeyValuePair<Guid, BillDto>(Guid.NewGuid(), new BillDto { Id = Guid.NewGuid(), Institution = "Electricity", Amount = 850 }),
            new KeyValuePair<Guid, BillDto>(Guid.NewGuid(), new BillDto { Id = Guid.NewGuid(), Institution = "Water", Amount = 320 })
        });

    /// <inheritdoc />
    public BillDto[] GetBills(string citizenNumber) => Bills.Values.Where(x => !x.IsPaid).ToArray();

    /// <inheritdoc />
    public PaymentResultDto PayBill(Guid billId)
    {
        if (!Bills.TryGetValue(billId, out var bill))
        {
            return new PaymentResultDto { Success = false, Message = "Bill not found." };
        }

        bill.IsPaid = true;
        return new PaymentResultDto { Success = true, Message = "Bill paid." };
    }
}

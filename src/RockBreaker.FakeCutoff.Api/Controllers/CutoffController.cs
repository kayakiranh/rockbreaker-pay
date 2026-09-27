using Microsoft.AspNetCore.Mvc;

namespace RockBreaker.FakeCutoff.Controllers;

/// <summary>
/// TR: Mesai saati ve resmi tatil benzeri cutoff kurallarını simüle eder.
/// EN: Simulates cutoff rules such as working hours and holidays.
/// Architecture: Fake Policy Service.
/// </summary>
[ApiController]
[Route("api/cutoff")]
public sealed class CutoffController : ControllerBase
{
    private static readonly HashSet<DateOnly> Holidays =
    [
        new(2026, 1, 1),
        new(2026, 4, 23),
        new(2026, 5, 1),
        new(2026, 5, 19),
        new(2026, 7, 15),
        new(2026, 8, 30),
        new(2026, 10, 29)
    ];

    /// <summary>
    /// TR: İşlem tipinin şu anda cutoff kurallarına göre çalışabilir olup olmadığını döndürür.
    /// EN: Returns whether the operation type is currently allowed by cutoff rules.
    /// Architecture: Policy Query.
    /// </summary>
    /// <param name="operationType">TR: İşlem tipi. EN: Operation type.</param>
    /// <returns>TR: Cutoff sonucu. EN: Cutoff result.</returns>
    [HttpGet("check")]
    public ActionResult<CutoffResponse> Check([FromQuery] string operationType)
    {
        if (string.Equals(operationType, "WalletToWallet", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new CutoffResponse { Allowed = true, Reason = "Wallet-to-wallet is available 24/7." });
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var date = DateOnly.FromDateTime(localNow);

        var isWeekend = localNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var isHoliday = Holidays.Contains(date);
        var isWorkingHour = localNow.TimeOfDay >= TimeSpan.FromHours(9)
            && localNow.TimeOfDay < TimeSpan.FromHours(17);

        var allowed = !isWeekend && !isHoliday && isWorkingHour;

        return Ok(new CutoffResponse
        {
            Allowed = allowed,
            Reason = allowed ? "Operation is within working hours." : "Operation is outside working hours or on a holiday."
        });
    }
}

/// <summary>
/// TR: Cutoff kontrol sonucudur.
/// EN: Cutoff check result.
/// Architecture: Response DTO.
/// </summary>
public sealed class CutoffResponse
{
    /// <summary>TR: İşlemin çalıştırılabilir olup olmadığı. EN: Whether the operation is allowed. Architecture: DTO Property.</summary>
    public bool Allowed { get; init; }
    /// <summary>TR: Kararın açıklaması. EN: Decision explanation. Architecture: Explainable Policy Result.</summary>
    public string Reason { get; init; } = string.Empty;
}

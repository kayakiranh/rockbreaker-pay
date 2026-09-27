using System.Data;
using Dapper;
using WalletEntity = RockBreaker.Pay.Modules.Wallet.Domain.Wallet;

namespace RockBreaker.Pay.Modules.Wallet.Application;

/// <summary>
/// TR: Tamamlanmış outgoing transaction'ları MSSQL'den toplayarak wallet kullanıcı limitlerini uygular.
/// EN: Enforces wallet user limits by aggregating completed outgoing transactions from MSSQL.
/// Architecture: Domain Policy + Transactionally Consistent Read Model.
/// </summary>
public sealed class WalletLimitGuard : IWalletLimitGuard
{
    /// <inheritdoc />
    public async Task<WalletLimitCheckResult> CheckAsync(
        WalletEntity wallet,
        decimal amount,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        if (amount > wallet.SingleTransactionLimit)
        {
            return WalletLimitCheckResult.Rejected(
                "SINGLE_LIMIT_EXCEEDED",
                "Single transaction limit was exceeded.");
        }

        const string sql = """
            SELECT
                COALESCE(SUM(CASE
                    WHEN CreatedAtUtc >= @DayStartUtc THEN Amount
                    ELSE 0
                END), 0) AS DailyAmount,
                COALESCE(SUM(CASE
                    WHEN CreatedAtUtc >= @MonthStartUtc THEN Amount
                    ELSE 0
                END), 0) AS MonthlyAmount
            FROM dbo.WalletTransactions
            WHERE SourceWalletId = @WalletId
              AND Status = 3
              AND CreatedAtUtc >= @MonthStartUtc;
            """;

        var now = DateTime.UtcNow;
        var dayStartUtc = now.Date;
        var monthStartUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var usage = await connection.QuerySingleAsync<WalletUsageRow>(
            sql,
            new
            {
                WalletId = wallet.Id,
                DayStartUtc = dayStartUtc,
                MonthStartUtc = monthStartUtc
            },
            transaction);

        if (usage.DailyAmount + amount > wallet.DailyLimit)
        {
            return WalletLimitCheckResult.Rejected(
                "DAILY_LIMIT_EXCEEDED",
                "Daily transaction limit was exceeded.");
        }

        if (usage.MonthlyAmount + amount > wallet.MonthlyLimit)
        {
            return WalletLimitCheckResult.Rejected(
                "MONTHLY_LIMIT_EXCEEDED",
                "Monthly transaction limit was exceeded.");
        }

        return WalletLimitCheckResult.Allowed();
    }

    /// <summary>
    /// TR: Günlük ve aylık outgoing kullanım toplamlarını taşır.
    /// EN: Carries daily and monthly outgoing usage totals.
    /// Architecture: Read Model DTO.
    /// </summary>
    private sealed class WalletUsageRow
    {
        /// <summary>TR: UTC gün içindeki toplam outgoing tutar. EN: Total outgoing amount in the UTC day. Architecture: Read Model Property.</summary>
        public decimal DailyAmount { get; init; }

        /// <summary>TR: UTC ay içindeki toplam outgoing tutar. EN: Total outgoing amount in the UTC month. Architecture: Read Model Property.</summary>
        public decimal MonthlyAmount { get; init; }
    }
}

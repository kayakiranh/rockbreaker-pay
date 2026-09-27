using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;

namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Aktif fraud kurallarını MSSQL'den okuyup transaction geçmişi üzerinde değerlendirir.
/// EN: Reads active fraud rules from MSSQL and evaluates them against transaction history.
/// Architecture: Rule Engine + Repository-less read model for a small, focused module.
/// </summary>
public sealed class DatabaseFraudEvaluator : IFraudEvaluator
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// TR: Fraud evaluator bağımlılıklarını alır.
    /// EN: Receives fraud evaluator dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    public DatabaseFraudEvaluator(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<FraudDecision> EvaluateAsync(Guid walletId, decimal amount)
    {
        const string ruleSql = """
            SELECT Code, RuleType, ThresholdValue, RiskScore, Action
            FROM dbo.FraudRules
            WHERE IsEnabled = 1;
            """;

        const string activitySql = """
            SELECT
                COUNT_BIG(1) AS DailyCount,
                COALESCE(SUM(Amount), 0) AS DailyAmount
            FROM dbo.WalletTransactions
            WHERE SourceWalletId = @WalletId
              AND CreatedAtUtc >= CONVERT(date, SYSUTCDATETIME())
              AND Status = 3;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rules = (await connection.QueryAsync<FraudRuleRow>(ruleSql)).ToArray();
        var activity = await connection.QuerySingleAsync<WalletActivityRow>(activitySql, new { WalletId = walletId });

        var triggered = new List<FraudRuleRow>();

        foreach (var rule in rules)
        {
            var isTriggered = rule.RuleType switch
            {
                "SingleTransactionAmount" => amount > rule.ThresholdValue,
                "DailyTransactionAmount" => activity.DailyAmount + amount > rule.ThresholdValue,
                "DailyTransactionCount" => activity.DailyCount + 1 > rule.ThresholdValue,
                _ => false
            };

            if (isTriggered)
            {
                triggered.Add(rule);
            }
        }

        if (triggered.Count == 0)
        {
            return new FraudDecision { Action = FraudAction.Allow, RiskScore = 0 };
        }

        return new FraudDecision
        {
            Action = (FraudAction)triggered.Max(x => x.Action),
            RiskScore = triggered.Sum(x => x.RiskScore),
            TriggeredRules = triggered.Select(x => x.Code).ToArray()
        };
    }

    private sealed class FraudRuleRow
    {
        public string Code { get; init; } = string.Empty;
        public string RuleType { get; init; } = string.Empty;
        public decimal ThresholdValue { get; init; }
        public int RiskScore { get; init; }
        public int Action { get; init; }
    }

    private sealed class WalletActivityRow
    {
        public long DailyCount { get; init; }
        public decimal DailyAmount { get; init; }
    }
}

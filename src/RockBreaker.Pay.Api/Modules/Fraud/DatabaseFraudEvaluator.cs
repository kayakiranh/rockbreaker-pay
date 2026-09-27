using System.Text.Json;
using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;

namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Aktif fraud kurallarını MSSQL'den okuyup transaction geçmişi üzerinde değerlendirir ve karar geçmişini kalıcılaştırır.
/// EN: Reads active fraud rules from MSSQL, evaluates them against transaction history and persists the decision history.
/// Architecture: Rule Engine + Explainable Decision Audit.
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
        var activity = await connection.QuerySingleAsync<WalletActivityRow>(
            activitySql,
            new { WalletId = walletId });

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

        var decision = CreateDecision(triggered);
        await PersistDecisionAsync(connection, walletId, amount, decision);
        return decision;
    }

    /// <summary>
    /// TR: Tetiklenen kural listesinden açıklanabilir fraud kararı üretir.
    /// EN: Creates an explainable fraud decision from the triggered-rule list.
    /// Architecture: Rule Engine Decision Aggregation.
    /// </summary>
    /// <param name="triggered">TR: Tetiklenen kurallar. EN: Triggered rules.</param>
    /// <returns>TR: Fraud kararı. EN: Fraud decision.</returns>
    private static FraudDecision CreateDecision(IReadOnlyCollection<FraudRuleRow> triggered)
    {
        if (triggered.Count == 0)
        {
            return new FraudDecision
            {
                Action = FraudAction.Allow,
                RiskScore = 0,
                TriggeredRules = Array.Empty<string>()
            };
        }

        return new FraudDecision
        {
            Action = (FraudAction)triggered.Max(x => x.Action),
            RiskScore = triggered.Sum(x => x.RiskScore),
            TriggeredRules = triggered.Select(x => x.Code).ToArray()
        };
    }

    /// <summary>
    /// TR: Fraud kararını denetim ve raporlama için değiştirilemez olay kaydı olarak saklar.
    /// EN: Persists the fraud decision as an event record for audit and reporting.
    /// Architecture: Append-Only Decision Audit.
    /// </summary>
    /// <param name="connection">TR: Veritabanı bağlantısı. EN: Database connection.</param>
    /// <param name="walletId">TR: Wallet kimliği. EN: Wallet identifier.</param>
    /// <param name="amount">TR: Değerlendirilen tutar. EN: Evaluated amount.</param>
    /// <param name="decision">TR: Fraud kararı. EN: Fraud decision.</param>
    private static Task PersistDecisionAsync(
        System.Data.IDbConnection connection,
        Guid walletId,
        decimal amount,
        FraudDecision decision)
    {
        const string sql = """
            INSERT INTO dbo.FraudEvents
                (Id, WalletId, Amount, RiskScore, Action, TriggeredRulesJson, CreatedAtUtc)
            VALUES
                (@Id, @WalletId, @Amount, @RiskScore, @Action, @TriggeredRulesJson, SYSUTCDATETIME());
            """;

        return connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            Amount = amount,
            decision.RiskScore,
            Action = (int)decision.Action,
            TriggeredRulesJson = JsonSerializer.Serialize(decision.TriggeredRules)
        });
    }

    /// <summary>
    /// TR: Fraud kural tablosundan okunan satırı temsil eder.
    /// EN: Represents a row read from the fraud-rule table.
    /// Architecture: Persistence DTO.
    /// </summary>
    private sealed class FraudRuleRow
    {
        /// <summary>TR: Kural kodu. EN: Rule code. Architecture: Persistence Property.</summary>
        public string Code { get; init; } = string.Empty;

        /// <summary>TR: Kural tipi. EN: Rule type. Architecture: Persistence Property.</summary>
        public string RuleType { get; init; } = string.Empty;

        /// <summary>TR: Eşik değeri. EN: Threshold value. Architecture: Persistence Property.</summary>
        public decimal ThresholdValue { get; init; }

        /// <summary>TR: Risk puanı. EN: Risk score. Architecture: Persistence Property.</summary>
        public int RiskScore { get; init; }

        /// <summary>TR: Aksiyon kodu. EN: Action code. Architecture: Persistence Property.</summary>
        public int Action { get; init; }
    }

    /// <summary>
    /// TR: Wallet'ın günlük fraud değerlendirmesinde kullanılan aktivite özetidir.
    /// EN: Activity summary used for the wallet's daily fraud evaluation.
    /// Architecture: Read Model DTO.
    /// </summary>
    private sealed class WalletActivityRow
    {
        /// <summary>TR: Günlük işlem sayısı. EN: Daily transaction count. Architecture: Read Model Property.</summary>
        public long DailyCount { get; init; }

        /// <summary>TR: Günlük işlem tutarı. EN: Daily transaction amount. Architecture: Read Model Property.</summary>
        public decimal DailyAmount { get; init; }
    }
}

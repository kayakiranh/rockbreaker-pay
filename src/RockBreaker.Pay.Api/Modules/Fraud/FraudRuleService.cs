using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;

namespace RockBreaker.Pay.Modules.Fraud;

/// <summary>
/// TR: Fraud kural yönetimini Dapper üzerinden basit ve açık SQL ile uygular.
/// EN: Implements fraud-rule management with simple and explicit SQL through Dapper.
/// Architecture: Application Service + Dapper Read/Write Model.
/// </summary>
public sealed class FraudRuleService : IFraudRuleService
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// TR: Fraud rule service bağımlılıklarını alır.
    /// EN: Receives fraud-rule-service dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    public FraudRuleService(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<FraudRuleResponse>> GetRulesAsync()
    {
        const string sql = """
            SELECT Id, Code, RuleType, ThresholdValue, RiskScore, Action, IsEnabled
            FROM dbo.FraudRules
            ORDER BY Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<FraudRuleRow>(sql);
        return rows.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<OperationResult<FraudRuleResponse>> UpdateAsync(
        int ruleId,
        UpdateFraudRuleRequest request)
    {
        if (request.ThresholdValue <= 0)
        {
            return OperationResult<FraudRuleResponse>.Fail(
                "INVALID_THRESHOLD",
                "Threshold value must be greater than zero.");
        }

        if (request.RiskScore is < 0 or > 100)
        {
            return OperationResult<FraudRuleResponse>.Fail(
                "INVALID_RISK_SCORE",
                "Risk score must be between 0 and 100.");
        }

        const string updateSql = """
            UPDATE dbo.FraudRules
            SET ThresholdValue = @ThresholdValue,
                RiskScore = @RiskScore,
                Action = @Action,
                IsEnabled = @IsEnabled
            WHERE Id = @RuleId;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(updateSql, new
        {
            RuleId = ruleId,
            request.ThresholdValue,
            request.RiskScore,
            Action = (int)request.Action,
            request.IsEnabled
        });

        if (affected == 0)
        {
            return OperationResult<FraudRuleResponse>.Fail(
                "FRAUD_RULE_NOT_FOUND",
                "Fraud rule was not found.");
        }

        const string selectSql = """
            SELECT Id, Code, RuleType, ThresholdValue, RiskScore, Action, IsEnabled
            FROM dbo.FraudRules
            WHERE Id = @RuleId;
            """;

        var row = await connection.QuerySingleAsync<FraudRuleRow>(selectSql, new { RuleId = ruleId });
        return OperationResult<FraudRuleResponse>.Success(Map(row));
    }

    /// <summary>
    /// TR: Persistence satırını dış API DTO'suna dönüştürür.
    /// EN: Maps the persistence row to the external API DTO.
    /// Architecture: Explicit Mapper Pattern.
    /// </summary>
    /// <param name="row">TR: DB fraud kural satırı. EN: DB fraud-rule row.</param>
    /// <returns>TR: API fraud kuralı. EN: API fraud rule.</returns>
    private static FraudRuleResponse Map(FraudRuleRow row) => new()
    {
        Id = row.Id,
        Code = row.Code,
        RuleType = row.RuleType,
        ThresholdValue = row.ThresholdValue,
        RiskScore = row.RiskScore,
        Action = (FraudAction)row.Action,
        IsEnabled = row.IsEnabled
    };

    /// <summary>
    /// TR: FraudRules tablosundan okunan persistence modelidir.
    /// EN: Persistence model read from the FraudRules table.
    /// Architecture: Persistence DTO.
    /// </summary>
    private sealed class FraudRuleRow
    {
        /// <summary>TR: Kural kimliği. EN: Rule identifier. Architecture: Persistence Property.</summary>
        public int Id { get; init; }

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

        /// <summary>TR: Aktiflik bilgisi. EN: Enabled flag. Architecture: Persistence Property.</summary>
        public bool IsEnabled { get; init; }
    }
}

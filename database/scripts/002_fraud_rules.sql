SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.FraudRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FraudRules
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FraudRules PRIMARY KEY,
        Code NVARCHAR(100) NOT NULL,
        RuleType NVARCHAR(100) NOT NULL,
        ThresholdValue DECIMAL(18,2) NOT NULL,
        RiskScore INT NOT NULL,
        Action INT NOT NULL,
        IsEnabled BIT NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_FraudRules_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UX_FraudRules_Code UNIQUE(Code)
    );

    INSERT INTO dbo.FraudRules(Code, RuleType, ThresholdValue, RiskScore, Action, IsEnabled)
    VALUES
        ('FRAUD_SINGLE_50000', 'SingleTransactionAmount', 50000, 90, 2, 1),
        ('FRAUD_DAILY_100000', 'DailyTransactionAmount', 100000, 70, 2, 1),
        ('FRAUD_DAILY_COUNT_20', 'DailyTransactionCount', 20, 40, 1, 1);
END;
GO

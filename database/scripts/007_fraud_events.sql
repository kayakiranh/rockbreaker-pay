SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.FraudEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FraudEvents
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FraudEvents PRIMARY KEY,
        WalletId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        RiskScore INT NOT NULL,
        Action INT NOT NULL,
        TriggeredRulesJson NVARCHAR(4000) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT FK_FraudEvents_Wallets FOREIGN KEY(WalletId) REFERENCES dbo.Wallets(Id)
    );

    CREATE INDEX IX_FraudEvents_WalletId_CreatedAtUtc
        ON dbo.FraudEvents(WalletId, CreatedAtUtc DESC);

    CREATE INDEX IX_FraudEvents_Action_CreatedAtUtc
        ON dbo.FraudEvents(Action, CreatedAtUtc DESC);
END;
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.MoneyRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MoneyRequests
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_MoneyRequests PRIMARY KEY,
        RequesterWalletId UNIQUEIDENTIFIER NOT NULL,
        RequestedFromWalletId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Note NVARCHAR(500) NULL,
        Status INT NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        ExpiresAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT FK_MoneyRequests_RequesterWallet FOREIGN KEY(RequesterWalletId) REFERENCES dbo.Wallets(Id),
        CONSTRAINT FK_MoneyRequests_RequestedFromWallet FOREIGN KEY(RequestedFromWalletId) REFERENCES dbo.Wallets(Id),
        CONSTRAINT CK_MoneyRequests_Amount CHECK (Amount > 0)
    );

    CREATE INDEX IX_MoneyRequests_RequesterWalletId ON dbo.MoneyRequests(RequesterWalletId, CreatedAtUtc DESC);
    CREATE INDEX IX_MoneyRequests_RequestedFromWalletId ON dbo.MoneyRequests(RequestedFromWalletId, CreatedAtUtc DESC);
END;
GO

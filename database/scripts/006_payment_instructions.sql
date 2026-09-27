SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.PaymentInstructions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentInstructions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PaymentInstructions PRIMARY KEY,
        SourceWalletId UNIQUEIDENTIFIER NOT NULL,
        DestinationWalletId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Frequency INT NOT NULL,
        Status INT NOT NULL,
        NextRunAtUtc DATETIME2(3) NULL,
        LastRunAtUtc DATETIME2(3) NULL,
        LastError NVARCHAR(2000) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT FK_PaymentInstructions_SourceWallet FOREIGN KEY(SourceWalletId) REFERENCES dbo.Wallets(Id),
        CONSTRAINT FK_PaymentInstructions_DestinationWallet FOREIGN KEY(DestinationWalletId) REFERENCES dbo.Wallets(Id),
        CONSTRAINT CK_PaymentInstructions_Amount CHECK (Amount > 0)
    );

    CREATE INDEX IX_PaymentInstructions_Due
        ON dbo.PaymentInstructions(Status, NextRunAtUtc)
        INCLUDE(SourceWalletId, DestinationWalletId, Amount, Frequency);
END;
GO

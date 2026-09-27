SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.PaymentInstructions', 'FailureCount') IS NULL
BEGIN
    ALTER TABLE dbo.PaymentInstructions
    ADD FailureCount INT NOT NULL
        CONSTRAINT DF_PaymentInstructions_FailureCount DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.PaymentInstructions', 'LockedAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.PaymentInstructions
    ADD LockedAtUtc DATETIME2(3) NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.PaymentInstructions')
      AND name = 'IX_PaymentInstructions_ProcessingLease'
)
BEGIN
    CREATE INDEX IX_PaymentInstructions_ProcessingLease
        ON dbo.PaymentInstructions(Status, LockedAtUtc)
        INCLUDE(NextRunAtUtc, FailureCount);
END;
GO

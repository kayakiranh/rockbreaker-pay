SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.KycEvents', 'ProviderVerificationId') IS NULL
BEGIN
    ALTER TABLE dbo.KycEvents
    ADD ProviderVerificationId UNIQUEIDENTIFIER NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.KycEvents')
      AND name = 'IX_KycEvents_ProviderVerificationId'
)
BEGIN
    CREATE INDEX IX_KycEvents_ProviderVerificationId
        ON dbo.KycEvents(ProviderVerificationId)
        WHERE ProviderVerificationId IS NOT NULL;
END;
GO

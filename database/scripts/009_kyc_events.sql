SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.KycEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KycEvents
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_KycEvents PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        PreviousStatus NVARCHAR(30) NOT NULL,
        NewStatus NVARCHAR(30) NOT NULL,
        ChangedByUserId UNIQUEIDENTIFIER NOT NULL,
        Reason NVARCHAR(1000) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT FK_KycEvents_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_KycEvents_ChangedByUser FOREIGN KEY(ChangedByUserId) REFERENCES dbo.Users(Id)
    );

    CREATE INDEX IX_KycEvents_UserId_CreatedAtUtc
        ON dbo.KycEvents(UserId, CreatedAtUtc DESC);
END;
GO

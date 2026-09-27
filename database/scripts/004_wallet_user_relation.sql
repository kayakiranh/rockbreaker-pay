SET XACT_ABORT ON;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Wallets_Users'
)
BEGIN
    ALTER TABLE dbo.Wallets
    ADD CONSTRAINT FK_Wallets_Users
        FOREIGN KEY(UserId) REFERENCES dbo.Users(Id);
END;
GO

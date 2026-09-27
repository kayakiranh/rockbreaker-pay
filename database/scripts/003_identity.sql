SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Email NVARCHAR(320) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Phone NVARCHAR(30) NULL,
        Role NVARCHAR(50) NOT NULL,
        KycStatus NVARCHAR(30) NOT NULL,
        SmsEnabled BIT NOT NULL,
        EmailEnabled BIT NOT NULL,
        PushEnabled BIT NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        UpdatedAtUtc DATETIME2(3) NOT NULL
    );
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users(Email);
END;
GO

IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        TokenHash CHAR(64) NOT NULL,
        ExpiresAtUtc DATETIME2(3) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        IsRevoked BIT NOT NULL,
        RevokedAtUtc DATETIME2(3) NULL,
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(Id)
    );
    CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens(TokenHash);
END;
GO

IF OBJECT_ID('dbo.PasswordResetTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PasswordResetTokens PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        TokenHash CHAR(64) NOT NULL,
        ExpiresAtUtc DATETIME2(3) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        IsUsed BIT NOT NULL,
        UsedAtUtc DATETIME2(3) NULL,
        CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(Id)
    );
    CREATE UNIQUE INDEX UX_PasswordResetTokens_TokenHash ON dbo.PasswordResetTokens(TokenHash);
END;
GO

SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.Wallets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Wallets
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Wallets PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Balance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Wallets_Balance DEFAULT (0),
        Currency CHAR(3) NOT NULL CONSTRAINT DF_Wallets_Currency DEFAULT ('TRY'),
        Status INT NOT NULL,
        SingleTransactionLimit DECIMAL(18,2) NOT NULL,
        DailyLimit DECIMAL(18,2) NOT NULL,
        MonthlyLimit DECIMAL(18,2) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        UpdatedAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT CK_Wallets_Balance CHECK (Balance >= 0),
        CONSTRAINT CK_Wallets_Currency CHECK (Currency = 'TRY')
    );

    CREATE UNIQUE INDEX UX_Wallets_UserId ON dbo.Wallets(UserId);
END;
GO

IF OBJECT_ID('dbo.WalletTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WalletTransactions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WalletTransactions PRIMARY KEY,
        SourceWalletId UNIQUEIDENTIFIER NULL,
        DestinationWalletId UNIQUEIDENTIFIER NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Currency CHAR(3) NOT NULL,
        Type NVARCHAR(50) NOT NULL,
        Status INT NOT NULL,
        IdempotencyKey NVARCHAR(100) NULL,
        CorrelationId NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        CompletedAtUtc DATETIME2(3) NULL,
        CONSTRAINT CK_WalletTransactions_Amount CHECK (Amount > 0),
        CONSTRAINT CK_WalletTransactions_Currency CHECK (Currency = 'TRY')
    );

    CREATE UNIQUE INDEX UX_WalletTransactions_IdempotencyKey
        ON dbo.WalletTransactions(IdempotencyKey)
        WHERE IdempotencyKey IS NOT NULL;
END;
GO

IF OBJECT_ID('dbo.LedgerEntries', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LedgerEntries
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LedgerEntries PRIMARY KEY,
        TransactionId UNIQUEIDENTIFIER NOT NULL,
        WalletId UNIQUEIDENTIFIER NOT NULL,
        Direction INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        BalanceAfter DECIMAL(18,2) NOT NULL,
        Currency CHAR(3) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        CONSTRAINT FK_LedgerEntries_Transaction FOREIGN KEY (TransactionId) REFERENCES dbo.WalletTransactions(Id),
        CONSTRAINT FK_LedgerEntries_Wallet FOREIGN KEY (WalletId) REFERENCES dbo.Wallets(Id),
        CONSTRAINT CK_LedgerEntries_Amount CHECK (Amount > 0),
        CONSTRAINT CK_LedgerEntries_BalanceAfter CHECK (BalanceAfter >= 0)
    );
END;
GO

IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        CorrelationId NVARCHAR(100) NOT NULL,
        TraceId NVARCHAR(100) NULL,
        UserId UNIQUEIDENTIFIER NULL,
        WalletId UNIQUEIDENTIFIER NULL,
        TransactionId UNIQUEIDENTIFIER NULL,
        HttpMethod NVARCHAR(10) NOT NULL,
        Path NVARCHAR(500) NOT NULL,
        RequestBody NVARCHAR(MAX) NULL,
        ResponseBody NVARCHAR(MAX) NULL,
        ResponseStatusCode INT NOT NULL,
        ResponseTimeMs BIGINT NOT NULL,
        IsSuccess BIT NOT NULL,
        ClientIp NVARCHAR(64) NULL,
        UserAgent NVARCHAR(1000) NULL,
        ErrorCode NVARCHAR(100) NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL
    );

    CREATE INDEX IX_AuditLogs_CorrelationId ON dbo.AuditLogs(CorrelationId);
    CREATE INDEX IX_AuditLogs_CreatedAtUtc ON dbo.AuditLogs(CreatedAtUtc DESC);
END;
GO

IF OBJECT_ID('dbo.OutboxMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboxMessages
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxMessages PRIMARY KEY,
        Type NVARCHAR(200) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL,
        ProcessedAtUtc DATETIME2(3) NULL,
        RetryCount INT NOT NULL CONSTRAINT DF_OutboxMessages_RetryCount DEFAULT (0),
        LastError NVARCHAR(2000) NULL
    );
END;
GO

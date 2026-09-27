using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Abstractions;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.MoneyRequests;

/// <summary>
/// TR: Borç isteği yaşam döngüsünü ve kabul edildiğinde wallet transferini yönetir.
/// EN: Manages the money-request lifecycle and wallet transfer when accepted.
/// Architecture: Application Service + State Transition + Reused Financial Command.
/// </summary>
public sealed class MoneyRequestService : IMoneyRequestService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTransferService _transferService;

    /// <summary>TR: Servis bağımlılıklarını alır. EN: Receives service dependencies. Architecture: Constructor Injection.</summary>
    public MoneyRequestService(
        IDbConnectionFactory connectionFactory,
        IWalletRepository walletRepository,
        IWalletTransferService transferService)
    {
        _connectionFactory = connectionFactory;
        _walletRepository = walletRepository;
        _transferService = transferService;
    }

    /// <inheritdoc />
    public async Task<OperationResult<MoneyRequestResponse>> CreateAsync(Guid userId, CreateMoneyRequest request)
    {
        if (request.Amount <= 0)
            return OperationResult<MoneyRequestResponse>.Fail("INVALID_AMOUNT", "Amount must be greater than zero.");

        var requester = await _walletRepository.GetByUserIdAsync(userId);
        if (requester is null)
            return OperationResult<MoneyRequestResponse>.Fail("WALLET_NOT_FOUND", "Requester wallet was not found.");

        if (requester.Id == request.RequestedFromWalletId)
            return OperationResult<MoneyRequestResponse>.Fail("INVALID_WALLET", "Money cannot be requested from the same wallet.");

        if (await _walletRepository.GetByIdAsync(request.RequestedFromWalletId) is null)
            return OperationResult<MoneyRequestResponse>.Fail("TARGET_WALLET_NOT_FOUND", "Requested-from wallet was not found.");

        var row = new MoneyRequestRow
        {
            Id = Guid.NewGuid(),
            RequesterWalletId = requester.Id,
            RequestedFromWalletId = request.RequestedFromWalletId,
            Amount = request.Amount,
            Note = request.Note,
            Status = (int)MoneyRequestStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        const string sql = """
            INSERT INTO dbo.MoneyRequests
                (Id, RequesterWalletId, RequestedFromWalletId, Amount, Note, Status, CreatedAtUtc, ExpiresAtUtc)
            VALUES
                (@Id, @RequesterWalletId, @RequestedFromWalletId, @Amount, @Note, @Status, @CreatedAtUtc, @ExpiresAtUtc);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, row);
        return OperationResult<MoneyRequestResponse>.Success(Map(row));
    }

    /// <inheritdoc />
    public async Task<OperationResult<IReadOnlyCollection<MoneyRequestResponse>>> GetMineAsync(Guid userId)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet is null)
            return OperationResult<IReadOnlyCollection<MoneyRequestResponse>>.Fail("WALLET_NOT_FOUND", "Wallet was not found.");

        const string sql = """
            SELECT Id, RequesterWalletId, RequestedFromWalletId, Amount, Note, Status, CreatedAtUtc, ExpiresAtUtc
            FROM dbo.MoneyRequests
            WHERE RequesterWalletId = @WalletId OR RequestedFromWalletId = @WalletId
            ORDER BY CreatedAtUtc DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<MoneyRequestRow>(sql, new { WalletId = wallet.Id })).ToArray();
        return OperationResult<IReadOnlyCollection<MoneyRequestResponse>>.Success(rows.Select(Map).ToArray());
    }

    /// <inheritdoc />
    public async Task<OperationResult<MoneyRequestResponse>> AcceptAsync(Guid userId, Guid requestId)
    {
        var currentWallet = await _walletRepository.GetByUserIdAsync(userId);
        if (currentWallet is null)
            return OperationResult<MoneyRequestResponse>.Fail("WALLET_NOT_FOUND", "Wallet was not found.");

        var row = await GetAsync(requestId);
        if (row is null)
            return OperationResult<MoneyRequestResponse>.Fail("REQUEST_NOT_FOUND", "Money request was not found.");

        if (row.Status != (int)MoneyRequestStatus.Pending)
            return OperationResult<MoneyRequestResponse>.Fail("REQUEST_NOT_PENDING", "Money request is not pending.");

        if (row.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await UpdateStatusAsync(row.Id, MoneyRequestStatus.Expired);
            return OperationResult<MoneyRequestResponse>.Fail("REQUEST_EXPIRED", "Money request has expired.");
        }

        if (row.RequestedFromWalletId != currentWallet.Id)
            return OperationResult<MoneyRequestResponse>.Fail("FORBIDDEN", "Only the requested-from wallet can accept this request.");

        var transfer = await _transferService.TransferAsync(
            new TransferRequest
            {
                SourceWalletId = row.RequestedFromWalletId,
                DestinationWalletId = row.RequesterWalletId,
                Amount = row.Amount,
                Currency = "TRY"
            },
            $"money-request:{row.Id}",
            $"money-request:{row.Id}");

        if (!transfer.IsSuccess)
            return OperationResult<MoneyRequestResponse>.Fail(
                transfer.ErrorCode ?? "TRANSFER_FAILED",
                transfer.ErrorMessage ?? "Transfer failed.");

        row.Status = (int)MoneyRequestStatus.Accepted;
        await UpdateStatusAsync(row.Id, MoneyRequestStatus.Accepted);
        return OperationResult<MoneyRequestResponse>.Success(Map(row));
    }

    /// <inheritdoc />
    public Task<OperationResult<MoneyRequestResponse>> RejectAsync(Guid userId, Guid requestId) =>
        ChangeStatusAsync(userId, requestId, MoneyRequestStatus.Rejected, requesterMustOwn: false);

    /// <inheritdoc />
    public Task<OperationResult<MoneyRequestResponse>> CancelAsync(Guid userId, Guid requestId) =>
        ChangeStatusAsync(userId, requestId, MoneyRequestStatus.Cancelled, requesterMustOwn: true);

    private async Task<OperationResult<MoneyRequestResponse>> ChangeStatusAsync(
        Guid userId,
        Guid requestId,
        MoneyRequestStatus newStatus,
        bool requesterMustOwn)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        var row = await GetAsync(requestId);

        if (wallet is null || row is null)
            return OperationResult<MoneyRequestResponse>.Fail("REQUEST_NOT_FOUND", "Wallet or money request was not found.");

        if (row.Status != (int)MoneyRequestStatus.Pending)
            return OperationResult<MoneyRequestResponse>.Fail("REQUEST_NOT_PENDING", "Money request is not pending.");

        var allowed = requesterMustOwn
            ? row.RequesterWalletId == wallet.Id
            : row.RequestedFromWalletId == wallet.Id;

        if (!allowed)
            return OperationResult<MoneyRequestResponse>.Fail("FORBIDDEN", "Wallet is not allowed to change this request.");

        row.Status = (int)newStatus;
        await UpdateStatusAsync(row.Id, newStatus);
        return OperationResult<MoneyRequestResponse>.Success(Map(row));
    }

    private async Task<MoneyRequestRow?> GetAsync(Guid requestId)
    {
        const string sql = """
            SELECT Id, RequesterWalletId, RequestedFromWalletId, Amount, Note, Status, CreatedAtUtc, ExpiresAtUtc
            FROM dbo.MoneyRequests WHERE Id = @RequestId;
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MoneyRequestRow>(sql, new { RequestId = requestId });
    }

    private async Task UpdateStatusAsync(Guid requestId, MoneyRequestStatus status)
    {
        const string sql = "UPDATE dbo.MoneyRequests SET Status = @Status WHERE Id = @RequestId;";
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { RequestId = requestId, Status = (int)status });
    }

    private static MoneyRequestResponse Map(MoneyRequestRow row) => new()
    {
        Id = row.Id,
        RequesterWalletId = row.RequesterWalletId,
        RequestedFromWalletId = row.RequestedFromWalletId,
        Amount = row.Amount,
        Note = row.Note,
        Status = ((MoneyRequestStatus)row.Status).ToString(),
        CreatedAtUtc = row.CreatedAtUtc,
        ExpiresAtUtc = row.ExpiresAtUtc
    };

    private sealed class MoneyRequestRow
    {
        public Guid Id { get; init; }
        public Guid RequesterWalletId { get; init; }
        public Guid RequestedFromWalletId { get; init; }
        public decimal Amount { get; init; }
        public string? Note { get; init; }
        public int Status { get; set; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime ExpiresAtUtc { get; init; }
    }
}

using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Abstractions;

namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Otomatik talimat oluşturma, listeleme, pause/resume ve iptal işlemlerini yönetir.
/// EN: Manages creation, listing, pause/resume and cancellation of automatic instructions.
/// Architecture: Application Service + Dapper Persistence.
/// </summary>
public sealed class PaymentInstructionService : IPaymentInstructionService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWalletRepository _walletRepository;

    /// <summary>
    /// TR: Servis bağımlılıklarını alır.
    /// EN: Receives service dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public PaymentInstructionService(
        IDbConnectionFactory connectionFactory,
        IWalletRepository walletRepository)
    {
        _connectionFactory = connectionFactory;
        _walletRepository = walletRepository;
    }

    /// <inheritdoc />
    public async Task<OperationResult<PaymentInstructionResponse>> CreateAsync(
        Guid userId,
        CreatePaymentInstructionRequest request)
    {
        if (request.Amount <= 0)
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "INVALID_AMOUNT",
                "Amount must be greater than zero.");
        }

        if (!Enum.IsDefined(request.Frequency))
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "INVALID_FREQUENCY",
                "Payment instruction frequency is invalid.");
        }

        var source = await _walletRepository.GetByUserIdAsync(userId);
        var destination = await _walletRepository.GetByIdAsync(request.DestinationWalletId);

        if (source is null || destination is null)
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "WALLET_NOT_FOUND",
                "Source or destination wallet was not found.");
        }

        if (source.Id == destination.Id)
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "INVALID_WALLET",
                "Source and destination wallets must differ.");
        }

        var row = new PaymentInstructionRow
        {
            Id = Guid.NewGuid(),
            SourceWalletId = source.Id,
            DestinationWalletId = destination.Id,
            Amount = request.Amount,
            Frequency = (int)request.Frequency,
            Status = (int)PaymentInstructionStatus.Active,
            NextRunAtUtc = request.FirstRunAtUtc <= DateTime.UtcNow
                ? DateTime.UtcNow
                : request.FirstRunAtUtc,
            FailureCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        const string sql = """
            INSERT INTO dbo.PaymentInstructions
                (Id, SourceWalletId, DestinationWalletId, Amount, Frequency, Status,
                 NextRunAtUtc, FailureCount, CreatedAtUtc)
            VALUES
                (@Id, @SourceWalletId, @DestinationWalletId, @Amount, @Frequency, @Status,
                 @NextRunAtUtc, @FailureCount, @CreatedAtUtc);
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, row);

        return OperationResult<PaymentInstructionResponse>.Success(Map(row));
    }

    /// <inheritdoc />
    public async Task<OperationResult<IReadOnlyCollection<PaymentInstructionResponse>>> GetMineAsync(Guid userId)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        if (wallet is null)
        {
            return OperationResult<IReadOnlyCollection<PaymentInstructionResponse>>.Fail(
                "WALLET_NOT_FOUND",
                "Wallet was not found.");
        }

        const string sql = """
            SELECT Id, SourceWalletId, DestinationWalletId, Amount, Frequency, Status,
                   NextRunAtUtc, LastRunAtUtc, LastError, FailureCount, LockedAtUtc, CreatedAtUtc
            FROM dbo.PaymentInstructions
            WHERE SourceWalletId = @WalletId
            ORDER BY CreatedAtUtc DESC;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<PaymentInstructionRow>(
            sql,
            new { WalletId = wallet.Id })).ToArray();

        return OperationResult<IReadOnlyCollection<PaymentInstructionResponse>>.Success(
            rows.Select(Map).ToArray());
    }

    /// <inheritdoc />
    public Task<OperationResult<PaymentInstructionResponse>> PauseAsync(
        Guid userId,
        Guid instructionId) =>
        ChangeStatusAsync(userId, instructionId, PaymentInstructionStatus.Paused);

    /// <inheritdoc />
    public Task<OperationResult<PaymentInstructionResponse>> ResumeAsync(
        Guid userId,
        Guid instructionId) =>
        ChangeStatusAsync(userId, instructionId, PaymentInstructionStatus.Active);

    /// <inheritdoc />
    public Task<OperationResult<PaymentInstructionResponse>> CancelAsync(
        Guid userId,
        Guid instructionId) =>
        ChangeStatusAsync(userId, instructionId, PaymentInstructionStatus.Cancelled);

    /// <summary>
    /// TR: Talimatın sahipliğini ve mevcut state'ini kontrol ederek state transition uygular.
    /// EN: Applies a state transition after checking instruction ownership and current state.
    /// Architecture: Explicit State Transition.
    /// </summary>
    private async Task<OperationResult<PaymentInstructionResponse>> ChangeStatusAsync(
        Guid userId,
        Guid instructionId,
        PaymentInstructionStatus targetStatus)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        var row = await GetAsync(instructionId);

        if (wallet is null || row is null || row.SourceWalletId != wallet.Id)
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "INSTRUCTION_NOT_FOUND",
                "Instruction was not found.");
        }

        var current = (PaymentInstructionStatus)row.Status;

        if (current == PaymentInstructionStatus.Processing)
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "INSTRUCTION_PROCESSING",
                "Instruction is currently being processed.");
        }

        if (current is PaymentInstructionStatus.Cancelled or PaymentInstructionStatus.Completed)
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "INSTRUCTION_CLOSED",
                "Instruction is already closed.");
        }

        if (targetStatus == PaymentInstructionStatus.Paused &&
            current != PaymentInstructionStatus.Active)
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "INVALID_STATE",
                "Only active instructions can be paused.");
        }

        if (targetStatus == PaymentInstructionStatus.Active &&
            current is not (PaymentInstructionStatus.Paused or PaymentInstructionStatus.Failed))
        {
            return OperationResult<PaymentInstructionResponse>.Fail(
                "INVALID_STATE",
                "Only paused or failed instructions can be resumed.");
        }

        row.Status = (int)targetStatus;

        if (targetStatus == PaymentInstructionStatus.Cancelled)
        {
            row.NextRunAtUtc = null;
        }
        else if (targetStatus == PaymentInstructionStatus.Paused)
        {
            row.NextRunAtUtc = null;
        }
        else if (targetStatus == PaymentInstructionStatus.Active)
        {
            row.NextRunAtUtc = DateTime.UtcNow;
            row.FailureCount = 0;
            row.LastError = null;
        }

        const string sql = """
            UPDATE dbo.PaymentInstructions
            SET Status = @Status,
                NextRunAtUtc = @NextRunAtUtc,
                FailureCount = @FailureCount,
                LastError = @LastError,
                LockedAtUtc = NULL
            WHERE Id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, row);

        return OperationResult<PaymentInstructionResponse>.Success(Map(row));
    }

    /// <summary>
    /// TR: Talimatı kimliğine göre getirir.
    /// EN: Gets an instruction by identifier.
    /// Architecture: Persistence Query.
    /// </summary>
    private async Task<PaymentInstructionRow?> GetAsync(Guid id)
    {
        const string sql = """
            SELECT Id, SourceWalletId, DestinationWalletId, Amount, Frequency, Status,
                   NextRunAtUtc, LastRunAtUtc, LastError, FailureCount, LockedAtUtc, CreatedAtUtc
            FROM dbo.PaymentInstructions
            WHERE Id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PaymentInstructionRow>(
            sql,
            new { Id = id });
    }

    /// <summary>
    /// TR: Persistence modelini dış API modeline açık kodla dönüştürür.
    /// EN: Explicitly maps the persistence model to the external API model.
    /// Architecture: Explicit Mapper.
    /// </summary>
    private static PaymentInstructionResponse Map(PaymentInstructionRow row) => new()
    {
        Id = row.Id,
        SourceWalletId = row.SourceWalletId,
        DestinationWalletId = row.DestinationWalletId,
        Amount = row.Amount,
        Frequency = ((PaymentInstructionFrequency)row.Frequency).ToString(),
        Status = ((PaymentInstructionStatus)row.Status).ToString(),
        NextRunAtUtc = row.NextRunAtUtc,
        LastError = row.LastError,
        FailureCount = row.FailureCount
    };

    /// <summary>
    /// TR: PaymentInstructions tablosundaki talimat satırını temsil eder.
    /// EN: Represents an instruction row from PaymentInstructions.
    /// Architecture: Persistence DTO.
    /// </summary>
    internal sealed class PaymentInstructionRow
    {
        public Guid Id { get; init; }
        public Guid SourceWalletId { get; init; }
        public Guid DestinationWalletId { get; init; }
        public decimal Amount { get; init; }
        public int Frequency { get; init; }
        public int Status { get; set; }
        public DateTime? NextRunAtUtc { get; set; }
        public DateTime? LastRunAtUtc { get; set; }
        public string? LastError { get; set; }
        public int FailureCount { get; set; }
        public DateTime? LockedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; init; }
    }
}

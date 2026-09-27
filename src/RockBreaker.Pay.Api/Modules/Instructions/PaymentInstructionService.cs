using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Abstractions;

namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Otomatik talimat oluşturma, listeleme ve iptal işlemlerini yönetir.
/// EN: Manages creation, listing and cancellation of automatic instructions.
/// Architecture: Application Service + Dapper Persistence.
/// </summary>
public sealed class PaymentInstructionService : IPaymentInstructionService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWalletRepository _walletRepository;

    /// <summary>TR: Servis bağımlılıklarını alır. EN: Receives service dependencies. Architecture: Constructor Injection.</summary>
    public PaymentInstructionService(IDbConnectionFactory connectionFactory, IWalletRepository walletRepository)
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
            return OperationResult<PaymentInstructionResponse>.Fail("INVALID_AMOUNT", "Amount must be greater than zero.");

        var source = await _walletRepository.GetByUserIdAsync(userId);
        var destination = await _walletRepository.GetByIdAsync(request.DestinationWalletId);

        if (source is null || destination is null)
            return OperationResult<PaymentInstructionResponse>.Fail("WALLET_NOT_FOUND", "Source or destination wallet was not found.");

        if (source.Id == destination.Id)
            return OperationResult<PaymentInstructionResponse>.Fail("INVALID_WALLET", "Source and destination wallets must differ.");

        var row = new PaymentInstructionRow
        {
            Id = Guid.NewGuid(),
            SourceWalletId = source.Id,
            DestinationWalletId = destination.Id,
            Amount = request.Amount,
            Frequency = (int)request.Frequency,
            Status = (int)PaymentInstructionStatus.Active,
            NextRunAtUtc = request.FirstRunAtUtc <= DateTime.UtcNow ? DateTime.UtcNow : request.FirstRunAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        const string sql = """
            INSERT INTO dbo.PaymentInstructions
                (Id, SourceWalletId, DestinationWalletId, Amount, Frequency, Status,
                 NextRunAtUtc, CreatedAtUtc)
            VALUES
                (@Id, @SourceWalletId, @DestinationWalletId, @Amount, @Frequency, @Status,
                 @NextRunAtUtc, @CreatedAtUtc);
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
            return OperationResult<IReadOnlyCollection<PaymentInstructionResponse>>.Fail("WALLET_NOT_FOUND", "Wallet was not found.");

        const string sql = """
            SELECT Id, SourceWalletId, DestinationWalletId, Amount, Frequency, Status,
                   NextRunAtUtc, LastRunAtUtc, LastError, CreatedAtUtc
            FROM dbo.PaymentInstructions
            WHERE SourceWalletId = @WalletId
            ORDER BY CreatedAtUtc DESC;
            """;
        using var connection = _connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<PaymentInstructionRow>(sql, new { WalletId = wallet.Id })).ToArray();
        return OperationResult<IReadOnlyCollection<PaymentInstructionResponse>>.Success(rows.Select(Map).ToArray());
    }

    /// <inheritdoc />
    public async Task<OperationResult<PaymentInstructionResponse>> CancelAsync(Guid userId, Guid instructionId)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId);
        var row = await GetAsync(instructionId);

        if (wallet is null || row is null || row.SourceWalletId != wallet.Id)
            return OperationResult<PaymentInstructionResponse>.Fail("INSTRUCTION_NOT_FOUND", "Instruction was not found.");

        if (row.Status is (int)PaymentInstructionStatus.Cancelled or (int)PaymentInstructionStatus.Completed)
            return OperationResult<PaymentInstructionResponse>.Fail("INSTRUCTION_CLOSED", "Instruction is already closed.");

        row.Status = (int)PaymentInstructionStatus.Cancelled;
        row.NextRunAtUtc = null;

        const string sql = """
            UPDATE dbo.PaymentInstructions
            SET Status = @Status, NextRunAtUtc = NULL
            WHERE Id = @Id;
            """;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new { row.Id, row.Status });
        return OperationResult<PaymentInstructionResponse>.Success(Map(row));
    }

    private async Task<PaymentInstructionRow?> GetAsync(Guid id)
    {
        const string sql = """
            SELECT Id, SourceWalletId, DestinationWalletId, Amount, Frequency, Status,
                   NextRunAtUtc, LastRunAtUtc, LastError, CreatedAtUtc
            FROM dbo.PaymentInstructions WHERE Id = @Id;
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<PaymentInstructionRow>(sql, new { Id = id });
    }

    private static PaymentInstructionResponse Map(PaymentInstructionRow row) => new()
    {
        Id = row.Id,
        SourceWalletId = row.SourceWalletId,
        DestinationWalletId = row.DestinationWalletId,
        Amount = row.Amount,
        Frequency = ((PaymentInstructionFrequency)row.Frequency).ToString(),
        Status = ((PaymentInstructionStatus)row.Status).ToString(),
        NextRunAtUtc = row.NextRunAtUtc,
        LastError = row.LastError
    };

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
        public DateTime CreatedAtUtc { get; init; }
    }
}

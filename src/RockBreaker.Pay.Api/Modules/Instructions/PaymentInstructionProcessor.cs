using System.Data;
using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Zamanı gelen talimatları DB üzerinde atomik claim ederek finansal transfer use-case'i üzerinden çalıştırır.
/// EN: Atomically claims due instructions in the database and executes them through the financial transfer use case.
/// Architecture: Distributed Worker Claim + Background Processor + Idempotent Financial Command.
/// </summary>
public sealed class PaymentInstructionProcessor : IPaymentInstructionProcessor
{
    private const int MaximumFailureCount = 5;
    private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(5);

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWalletTransferService _transferService;

    /// <summary>
    /// TR: Processor bağımlılıklarını alır.
    /// EN: Receives processor dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public PaymentInstructionProcessor(
        IDbConnectionFactory connectionFactory,
        IWalletTransferService transferService)
    {
        _connectionFactory = connectionFactory;
        _transferService = transferService;
    }

    /// <inheritdoc />
    public async Task ProcessDueAsync(CancellationToken cancellationToken)
    {
        var rows = await ClaimDueAsync(cancellationToken);

        foreach (var row in rows)
        {
            await ProcessClaimedAsync(row, cancellationToken);
        }
    }

    /// <summary>
    /// TR: Stale claim'leri serbest bırakır ve zamanı gelen talimatları READPAST/UPDLOCK ile tek worker'a atomik olarak claim eder.
    /// EN: Releases stale claims and atomically claims due instructions for a single worker using READPAST/UPDLOCK.
    /// Architecture: Database Work Queue + Pessimistic Claim Lease.
    /// </summary>
    private async Task<IReadOnlyCollection<InstructionRow>> ClaimDueAsync(
        CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        const string releaseStaleSql = """
            UPDATE dbo.PaymentInstructions
            SET Status = @Active,
                LockedAtUtc = NULL,
                LastError = COALESCE(LastError, 'Processing lease expired; instruction returned to queue.')
            WHERE Status = @Processing
              AND LockedAtUtc < DATEADD(MINUTE, -5, SYSUTCDATETIME());
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            releaseStaleSql,
            new
            {
                Active = (int)PaymentInstructionStatus.Active,
                Processing = (int)PaymentInstructionStatus.Processing
            },
            transaction,
            cancellationToken: cancellationToken));

        const string claimSql = """
            ;WITH Due AS
            (
                SELECT TOP (50) *
                FROM dbo.PaymentInstructions WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status = @Active
                  AND NextRunAtUtc <= SYSUTCDATETIME()
                ORDER BY NextRunAtUtc, Id
            )
            UPDATE Due
            SET Status = @Processing,
                LockedAtUtc = SYSUTCDATETIME()
            OUTPUT
                inserted.Id,
                inserted.SourceWalletId,
                inserted.DestinationWalletId,
                inserted.Amount,
                inserted.Frequency,
                inserted.NextRunAtUtc,
                inserted.FailureCount;
            """;

        var rows = (await connection.QueryAsync<InstructionRow>(
            new CommandDefinition(
                claimSql,
                new
                {
                    Active = (int)PaymentInstructionStatus.Active,
                    Processing = (int)PaymentInstructionStatus.Processing
                },
                transaction,
                cancellationToken: cancellationToken))).ToArray();

        transaction.Commit();
        return rows;
    }

    /// <summary>
    /// TR: Claim edilmiş talimatı idempotent transfer komutu ile çalıştırır ve retry/terminal state kararını yazar.
    /// EN: Executes a claimed instruction using an idempotent transfer command and writes retry/terminal state.
    /// Architecture: Idempotent Command + Retry State Machine.
    /// </summary>
    private async Task ProcessClaimedAsync(
        InstructionRow row,
        CancellationToken cancellationToken)
    {
        var scheduledAt = row.NextRunAtUtc ?? DateTime.UtcNow;

        var result = await _transferService.TransferAsync(
            new TransferRequest
            {
                SourceWalletId = row.SourceWalletId,
                DestinationWalletId = row.DestinationWalletId,
                Amount = row.Amount,
                Currency = "TRY"
            },
            $"instruction:{row.Id}:{scheduledAt:O}",
            $"instruction:{row.Id}");

        if (result.IsSuccess)
        {
            await MarkSuccessAsync(row, scheduledAt, cancellationToken);
            return;
        }

        await MarkFailureAsync(row, result.ErrorMessage, cancellationToken);
    }

    /// <summary>
    /// TR: Başarılı talimatın bir sonraki çalışma zamanını veya Completed state'ini yazar.
    /// EN: Writes the next execution time or Completed state for a successful instruction.
    /// Architecture: State Transition.
    /// </summary>
    private async Task MarkSuccessAsync(
        InstructionRow row,
        DateTime scheduledAt,
        CancellationToken cancellationToken)
    {
        var isOnce = row.Frequency == (int)PaymentInstructionFrequency.Once;
        var nextRun = isOnce
            ? (DateTime?)null
            : CalculateNextRun(row.Frequency, scheduledAt);

        const string sql = """
            UPDATE dbo.PaymentInstructions
            SET Status = @Status,
                NextRunAtUtc = @NextRunAtUtc,
                LastRunAtUtc = SYSUTCDATETIME(),
                LastError = NULL,
                FailureCount = 0,
                LockedAtUtc = NULL
            WHERE Id = @Id
              AND Status = @Processing;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                row.Id,
                Status = (int)(isOnce
                    ? PaymentInstructionStatus.Completed
                    : PaymentInstructionStatus.Active),
                NextRunAtUtc = nextRun,
                Processing = (int)PaymentInstructionStatus.Processing
            },
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// TR: Başarısız talimatı bir saat sonra retry eder; beş ardışık hatadan sonra Failed state'ine alır.
    /// EN: Retries a failed instruction after one hour and moves it to Failed after five consecutive failures.
    /// Architecture: Bounded Retry Policy.
    /// </summary>
    private async Task MarkFailureAsync(
        InstructionRow row,
        string? error,
        CancellationToken cancellationToken)
    {
        var failureCount = row.FailureCount + 1;
        var terminalFailure = failureCount >= MaximumFailureCount;

        const string sql = """
            UPDATE dbo.PaymentInstructions
            SET Status = @Status,
                NextRunAtUtc = @NextRunAtUtc,
                LastRunAtUtc = SYSUTCDATETIME(),
                LastError = @LastError,
                FailureCount = @FailureCount,
                LockedAtUtc = NULL
            WHERE Id = @Id
              AND Status = @Processing;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                row.Id,
                Status = (int)(terminalFailure
                    ? PaymentInstructionStatus.Failed
                    : PaymentInstructionStatus.Active),
                NextRunAtUtc = terminalFailure
                    ? (DateTime?)null
                    : DateTime.UtcNow.AddHours(1),
                LastError = Truncate(error, 2000),
                FailureCount = failureCount,
                Processing = (int)PaymentInstructionStatus.Processing
            },
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// TR: Talimatın sıklığına göre bir sonraki çalışma zamanını hesaplar.
    /// EN: Calculates the next execution time based on instruction frequency.
    /// Architecture: Scheduling Policy.
    /// </summary>
    private static DateTime CalculateNextRun(int frequency, DateTime current) =>
        (PaymentInstructionFrequency)frequency switch
        {
            PaymentInstructionFrequency.Daily => current.AddDays(1),
            PaymentInstructionFrequency.Weekly => current.AddDays(7),
            PaymentInstructionFrequency.Monthly => current.AddMonths(1),
            _ => current
        };

    /// <summary>
    /// TR: DB kolon limitine göre hata mesajını kısaltır.
    /// EN: Truncates an error message to the database-column limit.
    /// Architecture: Persistence Guard.
    /// </summary>
    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength];

    /// <summary>
    /// TR: Worker'ın claim ettiği talimat read-model kaydıdır.
    /// EN: Read-model row for an instruction claimed by the worker.
    /// Architecture: Work Queue DTO.
    /// </summary>
    private sealed class InstructionRow
    {
        public Guid Id { get; init; }
        public Guid SourceWalletId { get; init; }
        public Guid DestinationWalletId { get; init; }
        public decimal Amount { get; init; }
        public int Frequency { get; init; }
        public DateTime? NextRunAtUtc { get; init; }
        public int FailureCount { get; init; }
    }
}

using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Wallet.Application;
using RockBreaker.Pay.Modules.Wallet.Contracts;

namespace RockBreaker.Pay.Modules.Instructions;

/// <summary>
/// TR: Zamanı gelen talimatları finansal transfer use-case'i üzerinden çalıştırır.
/// EN: Executes due instructions through the existing financial transfer use case.
/// Architecture: Background Processor + Reused Application Command.
/// </summary>
public sealed class PaymentInstructionProcessor : IPaymentInstructionProcessor
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IWalletTransferService _transferService;

    /// <summary>TR: Processor bağımlılıklarını alır. EN: Receives processor dependencies. Architecture: Constructor Injection.</summary>
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
        const string selectSql = """
            SELECT TOP (50)
                Id, SourceWalletId, DestinationWalletId, Amount, Frequency, Status,
                NextRunAtUtc, LastRunAtUtc, LastError, CreatedAtUtc
            FROM dbo.PaymentInstructions
            WHERE Status = @Active
              AND NextRunAtUtc <= SYSUTCDATETIME()
            ORDER BY NextRunAtUtc;
            """;

        using var connection = _connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<InstructionRow>(
            new CommandDefinition(
                selectSql,
                new { Active = (int)PaymentInstructionStatus.Active },
                cancellationToken: cancellationToken))).ToArray();

        foreach (var row in rows)
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

            var nextRun = result.IsSuccess ? CalculateNextRun(row.Frequency, scheduledAt) : scheduledAt.AddHours(1);
            var status = result.IsSuccess && row.Frequency == (int)PaymentInstructionFrequency.Once
                ? PaymentInstructionStatus.Completed
                : result.IsSuccess
                    ? PaymentInstructionStatus.Active
                    : PaymentInstructionStatus.Failed;

            const string updateSql = """
                UPDATE dbo.PaymentInstructions
                SET Status = @Status,
                    NextRunAtUtc = @NextRunAtUtc,
                    LastRunAtUtc = SYSUTCDATETIME(),
                    LastError = @LastError
                WHERE Id = @Id;
                """;

            await connection.ExecuteAsync(new CommandDefinition(
                updateSql,
                new
                {
                    row.Id,
                    Status = (int)status,
                    NextRunAtUtc = status == PaymentInstructionStatus.Completed ? null : nextRun,
                    LastError = result.IsSuccess ? null : result.ErrorMessage
                },
                cancellationToken: cancellationToken));
        }
    }

    private static DateTime CalculateNextRun(int frequency, DateTime current) =>
        (PaymentInstructionFrequency)frequency switch
        {
            PaymentInstructionFrequency.Daily => current.AddDays(1),
            PaymentInstructionFrequency.Weekly => current.AddDays(7),
            PaymentInstructionFrequency.Monthly => current.AddMonths(1),
            _ => current
        };

    private sealed class InstructionRow
    {
        public Guid Id { get; init; }
        public Guid SourceWalletId { get; init; }
        public Guid DestinationWalletId { get; init; }
        public decimal Amount { get; init; }
        public int Frequency { get; init; }
        public DateTime? NextRunAtUtc { get; init; }
    }
}

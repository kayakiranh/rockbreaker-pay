using System.Text.Json;
using Dapper;
using RockBreaker.Pay.Infrastructure.Persistence;

namespace RockBreaker.Pay.Modules.Notifications;

/// <summary>
/// TR: Wallet event'lerini outbox tablosundan okuyup kullanıcı tercihleri doğrultusunda notification servisine gönderir.
/// EN: Reads wallet events from the outbox table and sends notifications according to user preferences.
/// Architecture: Transactional Outbox + At-Least-Once Delivery + Retry.
/// </summary>
public sealed class OutboxNotificationProcessor : IOutboxNotificationProcessor
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly INotificationClient _notificationClient;
    private readonly ILogger<OutboxNotificationProcessor> _logger;

    /// <summary>
    /// TR: Processor bağımlılıklarını alır.
    /// EN: Receives processor dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="connectionFactory">TR: DB bağlantı fabrikası. EN: DB connection factory.</param>
    /// <param name="notificationClient">TR: Notification adapter'ı. EN: Notification adapter.</param>
    /// <param name="logger">TR: Uygulama logger'ı. EN: Application logger.</param>
    public OutboxNotificationProcessor(
        IDbConnectionFactory connectionFactory,
        INotificationClient notificationClient,
        ILogger<OutboxNotificationProcessor> logger)
    {
        _connectionFactory = connectionFactory;
        _notificationClient = notificationClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var messages = await GetMessagesAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await ProcessMessageAsync(message, cancellationToken);
                await MarkProcessedAsync(message.Id, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Outbox notification processing failed. OutboxId: {OutboxId}",
                    message.Id);

                await MarkFailedAsync(message.Id, exception.Message, cancellationToken);
            }
        }
    }

    /// <summary>
    /// TR: İşlenmemiş ve retry limiti aşılmamış notification event'lerini getirir.
    /// EN: Retrieves unprocessed notification events that have not exceeded the retry limit.
    /// Architecture: Outbox Read Model.
    /// </summary>
    /// <param name="cancellationToken">TR: İptal token'ı. EN: Cancellation token.</param>
    /// <returns>TR: Outbox mesajları. EN: Outbox messages.</returns>
    private async Task<IReadOnlyCollection<OutboxRow>> GetMessagesAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (50) Id, Type, Payload, RetryCount
            FROM dbo.OutboxMessages WITH (READPAST)
            WHERE ProcessedAtUtc IS NULL
              AND RetryCount < 10
              AND Type IN ('WalletTransferCompleted', 'BillPaymentCompleted')
            ORDER BY CreatedAtUtc;
            """;

        using var connection = _connectionFactory.CreateConnection();
        return (await connection.QueryAsync<OutboxRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken))).ToArray();
    }

    /// <summary>
    /// TR: Tek bir wallet transfer event'i için ilgili kullanıcılara tercih edilen kanallardan bildirim gönderir.
    /// EN: Sends notifications through preferred channels to users related to one wallet-transfer event.
    /// Architecture: Event Handler + External Adapter.
    /// </summary>
    /// <param name="message">TR: Outbox mesajı. EN: Outbox message.</param>
    /// <param name="cancellationToken">TR: İptal token'ı. EN: Cancellation token.</param>
    private async Task ProcessMessageAsync(OutboxRow message, CancellationToken cancellationToken)
    {
        if (string.Equals(message.Type, "WalletTransferCompleted", StringComparison.Ordinal))
        {
            var payload = JsonSerializer.Deserialize<WalletTransferPayload>(message.Payload)
                ?? throw new InvalidOperationException("Wallet-transfer outbox payload cannot be deserialized.");

            var recipients = await GetRecipientsAsync(
                payload.SourceWalletId,
                payload.DestinationWalletId,
                cancellationToken);

            foreach (var recipient in recipients)
            {
                var direction = recipient.WalletId == payload.SourceWalletId ? "gönderildi" : "alındı";
                var text = $"{payload.Amount:0.00} {payload.Currency} wallet transferi {direction}. İşlem: {payload.TransactionId}";
                await SendToRecipientAsync(recipient, text);
            }

            return;
        }

        if (string.Equals(message.Type, "BillPaymentCompleted", StringComparison.Ordinal))
        {
            var payload = JsonSerializer.Deserialize<BillPaymentPayload>(message.Payload)
                ?? throw new InvalidOperationException("Bill-payment outbox payload cannot be deserialized.");

            var recipients = await GetRecipientsAsync(
                payload.WalletId,
                payload.WalletId,
                cancellationToken);

            foreach (var recipient in recipients)
            {
                var text = $"{payload.Amount:0.00} {payload.Currency} fatura ödemesi tamamlandı. Fatura: {payload.BillId}, İşlem: {payload.TransactionId}";
                await SendToRecipientAsync(recipient, text);
            }

            return;
        }

        throw new InvalidOperationException($"Unsupported outbox message type: {message.Type}");
    }

    /// <summary>
    /// TR: Kullanıcının tercih ettiği aktif kanallara notification gönderir.
    /// EN: Sends a notification through the user's enabled preferred channels.
    /// Architecture: Notification Preference Policy + External Adapter.
    /// </summary>
    /// <param name="recipient">TR: Bildirim alıcısı. EN: Notification recipient.</param>
    /// <param name="text">TR: Bildirim metni. EN: Notification text.</param>
    private async Task SendToRecipientAsync(RecipientRow recipient, string text)
    {
        if (recipient.EmailEnabled && !string.IsNullOrWhiteSpace(recipient.Email))
        {
            await EnsureSentAsync("EMAIL", recipient.Email, text);
        }

        if (recipient.SmsEnabled && !string.IsNullOrWhiteSpace(recipient.Phone))
        {
            await EnsureSentAsync("SMS", recipient.Phone, text);
        }

        if (recipient.PushEnabled)
        {
            await EnsureSentAsync("PUSH", recipient.UserId.ToString(), text);
        }
    }

    /// <summary>
    /// TR: Notification servisinden başarısız sonuç dönerse retry mekanizmasını tetiklemek için exception üretir.
    /// EN: Throws when notification delivery fails so the retry mechanism can be triggered.
    /// Architecture: Retry Signaling.
    /// </summary>
    /// <param name="channel">TR: Kanal. EN: Channel.</param>
    /// <param name="recipient">TR: Alıcı. EN: Recipient.</param>
    /// <param name="message">TR: Mesaj. EN: Message.</param>
    private async Task EnsureSentAsync(string channel, string recipient, string message)
    {
        if (!await _notificationClient.SendAsync(channel, recipient, message))
        {
            throw new InvalidOperationException($"Notification service rejected {channel} delivery.");
        }
    }

    /// <summary>
    /// TR: Wallet sahiplerini ve notification tercihlerini getirir.
    /// EN: Retrieves wallet owners and their notification preferences.
    /// Architecture: Dapper Read Model.
    /// </summary>
    /// <param name="sourceWalletId">TR: Kaynak wallet. EN: Source wallet.</param>
    /// <param name="destinationWalletId">TR: Hedef wallet. EN: Destination wallet.</param>
    /// <param name="cancellationToken">TR: İptal token'ı. EN: Cancellation token.</param>
    /// <returns>TR: Bildirim alıcıları. EN: Notification recipients.</returns>
    private async Task<IReadOnlyCollection<RecipientRow>> GetRecipientsAsync(
        Guid sourceWalletId,
        Guid destinationWalletId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                w.Id AS WalletId,
                u.Id AS UserId,
                u.Email,
                u.Phone,
                u.SmsEnabled,
                u.EmailEnabled,
                u.PushEnabled
            FROM dbo.Wallets w
            INNER JOIN dbo.Users u ON u.Id = w.UserId
            WHERE w.Id IN (@SourceWalletId, @DestinationWalletId);
            """;

        using var connection = _connectionFactory.CreateConnection();
        return (await connection.QueryAsync<RecipientRow>(
            new CommandDefinition(
                sql,
                new { SourceWalletId = sourceWalletId, DestinationWalletId = destinationWalletId },
                cancellationToken: cancellationToken))).ToArray();
    }

    /// <summary>
    /// TR: Outbox event'ini başarıyla işlendi olarak işaretler.
    /// EN: Marks an outbox event as successfully processed.
    /// Architecture: Outbox State Transition.
    /// </summary>
    /// <param name="id">TR: Outbox kimliği. EN: Outbox identifier.</param>
    /// <param name="cancellationToken">TR: İptal token'ı. EN: Cancellation token.</param>
    private async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.OutboxMessages
            SET ProcessedAtUtc = SYSUTCDATETIME(), LastError = NULL
            WHERE Id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// TR: Başarısız event'in retry sayısını ve son hatasını günceller.
    /// EN: Updates retry count and last error for a failed event.
    /// Architecture: Retry State Transition.
    /// </summary>
    /// <param name="id">TR: Outbox kimliği. EN: Outbox identifier.</param>
    /// <param name="error">TR: Son hata. EN: Last error.</param>
    /// <param name="cancellationToken">TR: İptal token'ı. EN: Cancellation token.</param>
    private async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE dbo.OutboxMessages
            SET RetryCount = RetryCount + 1, LastError = @Error
            WHERE Id = @Id;
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id, Error = error.Length <= 2000 ? error : error[..2000] },
            cancellationToken: cancellationToken));
    }

    /// <summary>
    /// TR: Outbox tablosundan okunan transport kaydıdır.
    /// EN: Transport row read from the outbox table.
    /// Architecture: Persistence DTO.
    /// </summary>
    private sealed class OutboxRow
    {
        /// <summary>TR: Outbox kimliği. EN: Outbox identifier. Architecture: Persistence Property.</summary>
        public Guid Id { get; init; }

        /// <summary>TR: Event tipi. EN: Event type. Architecture: Persistence Property.</summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>TR: JSON payload. EN: JSON payload. Architecture: Persistence Property.</summary>
        public string Payload { get; init; } = string.Empty;

        /// <summary>TR: Retry sayısı. EN: Retry count. Architecture: Persistence Property.</summary>
        public int RetryCount { get; init; }
    }

    /// <summary>
    /// TR: Wallet transfer event payload modelidir.
    /// EN: Wallet transfer event payload model.
    /// Architecture: Event DTO.
    /// </summary>
    private sealed class BillPaymentPayload
    {
        /// <summary>TR: İşlem kimliği. EN: Transaction identifier. Architecture: Event Property.</summary>
        public Guid TransactionId { get; init; }

        /// <summary>TR: Ödeme yapan wallet kimliği. EN: Paying wallet identifier. Architecture: Event Property.</summary>
        public Guid WalletId { get; init; }

        /// <summary>TR: Fatura kimliği. EN: Bill identifier. Architecture: Event Property.</summary>
        public Guid BillId { get; init; }

        /// <summary>TR: Ödenen tutar. EN: Paid amount. Architecture: Event Property.</summary>
        public decimal Amount { get; init; }

        /// <summary>TR: Para birimi. EN: Currency. Architecture: Event Property.</summary>
        public string Currency { get; init; } = "TRY";
    }

    /// <summary>
    /// TR: Wallet transfer event payload modelidir.
    /// EN: Wallet transfer event payload model.
    /// Architecture: Event DTO.
    /// </summary>
    private sealed class WalletTransferPayload
    {
        /// <summary>TR: İşlem kimliği. EN: Transaction identifier. Architecture: Event Property.</summary>
        public Guid TransactionId { get; init; }

        /// <summary>TR: Kaynak wallet. EN: Source wallet. Architecture: Event Property.</summary>
        public Guid SourceWalletId { get; init; }

        /// <summary>TR: Hedef wallet. EN: Destination wallet. Architecture: Event Property.</summary>
        public Guid DestinationWalletId { get; init; }

        /// <summary>TR: Tutar. EN: Amount. Architecture: Event Property.</summary>
        public decimal Amount { get; init; }

        /// <summary>TR: Para birimi. EN: Currency. Architecture: Event Property.</summary>
        public string Currency { get; init; } = "TRY";
    }

    /// <summary>
    /// TR: Notification gönderilecek kullanıcı read-model kaydıdır.
    /// EN: Read-model row for a notification recipient.
    /// Architecture: Read Model DTO.
    /// </summary>
    private sealed class RecipientRow
    {
        /// <summary>TR: Wallet kimliği. EN: Wallet identifier. Architecture: Read Model Property.</summary>
        public Guid WalletId { get; init; }

        /// <summary>TR: Kullanıcı kimliği. EN: User identifier. Architecture: Read Model Property.</summary>
        public Guid UserId { get; init; }

        /// <summary>TR: E-mail. EN: E-mail. Architecture: Read Model Property.</summary>
        public string Email { get; init; } = string.Empty;

        /// <summary>TR: Telefon. EN: Phone. Architecture: Read Model Property.</summary>
        public string? Phone { get; init; }

        /// <summary>TR: SMS tercihi. EN: SMS preference. Architecture: Read Model Property.</summary>
        public bool SmsEnabled { get; init; }

        /// <summary>TR: E-mail tercihi. EN: E-mail preference. Architecture: Read Model Property.</summary>
        public bool EmailEnabled { get; init; }

        /// <summary>TR: Push tercihi. EN: Push preference. Architecture: Read Model Property.</summary>
        public bool PushEnabled { get; init; }
    }
}

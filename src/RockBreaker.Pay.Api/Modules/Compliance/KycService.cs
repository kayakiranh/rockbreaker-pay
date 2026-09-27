using Dapper;
using RockBreaker.Pay.Common;
using RockBreaker.Pay.Infrastructure.Persistence;
using RockBreaker.Pay.Modules.Identity.Abstractions;

namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: KYC state transition'larını ve değişiklik audit geçmişini yönetir.
/// EN: Manages KYC state transitions and change-audit history.
/// Architecture: Application Service + Append-Only Compliance Audit.
/// </summary>
public sealed class KycService : IKycService
{
    private readonly IUserRepository _userRepository;
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// TR: KYC service bağımlılıklarını alır.
    /// EN: Receives KYC-service dependencies.
    /// Architecture: Constructor Injection.
    /// </summary>
    public KycService(
        IUserRepository userRepository,
        IDbConnectionFactory connectionFactory)
    {
        _userRepository = userRepository;
        _connectionFactory = connectionFactory;
    }

    /// <inheritdoc />
    public async Task<OperationResult<KycStatusResponse>> GetAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);

        return user is null
            ? OperationResult<KycStatusResponse>.Fail("USER_NOT_FOUND", "User was not found.")
            : OperationResult<KycStatusResponse>.Success(
                new KycStatusResponse { UserId = user.Id, Status = user.KycStatus });
    }

    /// <inheritdoc />
    public async Task<OperationResult<KycStatusResponse>> SubmitAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return OperationResult<KycStatusResponse>.Fail(
                "USER_NOT_FOUND",
                "User was not found.");
        }

        if (user.KycStatus == KycStates.Pending)
        {
            return OperationResult<KycStatusResponse>.Fail(
                "KYC_ALREADY_PENDING",
                "KYC is already pending review.");
        }

        if (user.KycStatus == KycStates.Verified)
        {
            return OperationResult<KycStatusResponse>.Fail(
                "KYC_ALREADY_VERIFIED",
                "KYC is already verified.");
        }

        var previousStatus = user.KycStatus;
        await _userRepository.UpdateKycStatusAsync(user.Id, KycStates.Pending);

        await InsertEventAsync(
            user.Id,
            previousStatus,
            KycStates.Pending,
            user.Id,
            "Submitted by user.");

        return OperationResult<KycStatusResponse>.Success(
            new KycStatusResponse { UserId = user.Id, Status = KycStates.Pending });
    }

    /// <inheritdoc />
    public async Task<OperationResult<KycStatusResponse>> ReviewAsync(
        Guid actorUserId,
        Guid targetUserId,
        ReviewKycRequest request)
    {
        if (request.Status is not (KycStates.Verified or KycStates.Rejected))
        {
            return OperationResult<KycStatusResponse>.Fail(
                "INVALID_KYC_DECISION",
                "Admin review status must be Verified or Rejected.");
        }

        if (request.Status == KycStates.Rejected &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return OperationResult<KycStatusResponse>.Fail(
                "KYC_REJECTION_REASON_REQUIRED",
                "A rejection reason is required.");
        }

        var user = await _userRepository.GetByIdAsync(targetUserId);
        if (user is null)
        {
            return OperationResult<KycStatusResponse>.Fail(
                "USER_NOT_FOUND",
                "User was not found.");
        }

        if (user.KycStatus != KycStates.Pending)
        {
            return OperationResult<KycStatusResponse>.Fail(
                "KYC_NOT_PENDING",
                "Only Pending KYC can be reviewed.");
        }

        var previousStatus = user.KycStatus;
        await _userRepository.UpdateKycStatusAsync(user.Id, request.Status);

        await InsertEventAsync(
            user.Id,
            previousStatus,
            request.Status,
            actorUserId,
            request.Reason);

        return OperationResult<KycStatusResponse>.Success(
            new KycStatusResponse { UserId = user.Id, Status = request.Status });
    }

    private async Task InsertEventAsync(
        Guid userId,
        string previousStatus,
        string newStatus,
        Guid changedByUserId,
        string? reason)
    {
        const string sql = """
            INSERT INTO dbo.KycEvents
                (Id, UserId, PreviousStatus, NewStatus, ChangedByUserId, Reason, CreatedAtUtc)
            VALUES
                (@Id, @UserId, @PreviousStatus, @NewStatus, @ChangedByUserId, @Reason, SYSUTCDATETIME());
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            Reason = reason
        });
    }
}

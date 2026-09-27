using RockBreaker.Pay.Common;
using RockBreaker.Pay.Modules.Compliance;
using RockBreaker.Pay.Modules.Wallet.Application;

namespace RockBreaker.Pay.Modules.Campaigns;

/// <summary>
/// TR: Kampanya katılımını FakeCampaign ile, finansal transferi ise mevcut güvenli wallet transfer servisi ile orkestre eder.
/// EN: Orchestrates campaign participation through FakeCampaign and financial transfer through the existing secure wallet-transfer service.
/// Architecture: Application Service + Anti-Corruption Layer + Use-Case Composition.
/// </summary>
public sealed class CampaignService : ICampaignService
{
    private readonly ICampaignClient _campaignClient;
    private readonly IKycGuard _kycGuard;
    private readonly IWalletTransferService _walletTransferService;

    /// <summary>TR: Kampanya servis bağımlılıklarını alır. EN: Receives campaign-service dependencies. Architecture: Constructor Injection.</summary>
    public CampaignService(
        ICampaignClient campaignClient,
        IKycGuard kycGuard,
        IWalletTransferService walletTransferService)
    {
        _campaignClient = campaignClient;
        _kycGuard = kycGuard;
        _walletTransferService = walletTransferService;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<CampaignResponse>> GetAsync() =>
        _campaignClient.GetCampaignsAsync();

    /// <inheritdoc />
    public async Task<OperationResult<CampaignParticipationResponse>> JoinAsync(Guid userId, Guid campaignId)
    {
        if (!await _kycGuard.IsUserVerifiedAsync(userId))
        {
            return OperationResult<CampaignParticipationResponse>.Fail(
                "KYC_REQUIRED",
                "Verified KYC is required to join a campaign.");
        }

        var campaign = (await _campaignClient.GetCampaignsAsync()).FirstOrDefault(x => x.Id == campaignId);
        if (campaign is null)
        {
            return OperationResult<CampaignParticipationResponse>.Fail(
                "CAMPAIGN_NOT_FOUND",
                "Campaign was not found.");
        }

        if (!await _campaignClient.JoinAsync(campaignId, userId))
        {
            return OperationResult<CampaignParticipationResponse>.Fail(
                "CAMPAIGN_JOIN_FAILED",
                "Campaign provider rejected the participation request.");
        }

        return OperationResult<CampaignParticipationResponse>.Success(new CampaignParticipationResponse
        {
            CampaignId = campaignId,
            UserId = userId,
            IsJoined = true
        });
    }

    /// <inheritdoc />
    public async Task<OperationResult<CampaignTransferResponse>> TransferAsync(
        Guid userId,
        Guid campaignId,
        CampaignTransferRequest request,
        string idempotencyKey,
        string correlationId)
    {
        if (!await _campaignClient.IsJoinedAsync(campaignId, userId))
        {
            return OperationResult<CampaignTransferResponse>.Fail(
                "CAMPAIGN_PARTICIPATION_REQUIRED",
                "User must join the campaign before making a campaign transfer.");
        }

        var campaign = (await _campaignClient.GetCampaignsAsync()).FirstOrDefault(x => x.Id == campaignId);
        if (campaign is null)
        {
            return OperationResult<CampaignTransferResponse>.Fail(
                "CAMPAIGN_NOT_FOUND",
                "Campaign was not found.");
        }

        if (request.Amount < campaign.MinimumAmount)
        {
            return OperationResult<CampaignTransferResponse>.Fail(
                "CAMPAIGN_MINIMUM_AMOUNT_NOT_MET",
                $"Campaign minimum amount is {campaign.MinimumAmount:0.00} TRY.");
        }

        var transfer = await _walletTransferService.TransferForUserAsync(
            userId,
            request.ToTransferRequest(),
            idempotencyKey,
            correlationId);

        if (!transfer.IsSuccess)
        {
            return OperationResult<CampaignTransferResponse>.Fail(
                transfer.ErrorCode ?? "TRANSFER_FAILED",
                transfer.ErrorMessage ?? "Campaign transfer failed.");
        }

        return OperationResult<CampaignTransferResponse>.Success(new CampaignTransferResponse
        {
            CampaignId = campaignId,
            CampaignCashbackAmount = campaign.CashbackAmount,
            Transfer = transfer.Data!
        });
    }
}

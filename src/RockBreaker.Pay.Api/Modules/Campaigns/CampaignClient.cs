using System.Net;
using System.Net.Http.Json;

namespace RockBreaker.Pay.Modules.Campaigns;

/// <summary>
/// TR: FakeCampaign REST servisine typed HttpClient ile erişir ve dış modeli explicit mapping ile iç modele dönüştürür.
/// EN: Accesses FakeCampaign REST service with a typed HttpClient and explicitly maps the external model to the internal model.
/// Architecture: HTTP Adapter + Anti-Corruption Layer + Explicit Mapper.
/// </summary>
public sealed class CampaignClient : ICampaignClient
{
    private readonly HttpClient _httpClient;

    /// <summary>TR: Typed HttpClient bağımlılığını alır. EN: Receives the typed HttpClient dependency. Architecture: Constructor Injection.</summary>
    public CampaignClient(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<CampaignResponse>> GetCampaignsAsync()
    {
        var externalCampaigns = await _httpClient.GetFromJsonAsync<ExternalCampaign[]>("api/campaigns") ?? [];
        return externalCampaigns.Select(Map).ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> JoinAsync(Guid campaignId, Guid userId)
    {
        using var response = await _httpClient.PostAsync(
            $"api/campaigns/{campaignId}/participants/{userId}",
            content: null);

        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> IsJoinedAsync(Guid campaignId, Guid userId)
    {
        using var response = await _httpClient.GetAsync(
            $"api/campaigns/{campaignId}/participants/{userId}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        var status = await response.Content.ReadFromJsonAsync<ExternalParticipationStatus>();
        return status?.IsJoined == true;
    }

    /// <summary>TR: Dış servis modelini wallet API response modeline açık kodla dönüştürür. EN: Explicitly maps the external model to the wallet API model. Architecture: Explicit Mapper.</summary>
    private static CampaignResponse Map(ExternalCampaign campaign) => new()
    {
        Id = campaign.Id,
        Company = campaign.Company,
        Title = campaign.Title,
        MinimumAmount = campaign.MinimumAmount,
        CashbackAmount = campaign.CashbackAmount
    };

    /// <summary>TR: FakeCampaign transport modelidir. EN: FakeCampaign transport model. Architecture: Anti-Corruption Transport Model.</summary>
    private sealed class ExternalCampaign
    {
        /// <summary>TR: Kampanya kimliği. EN: Campaign identifier. Architecture: Transport Property.</summary>
        public Guid Id { get; init; }
        /// <summary>TR: Firma adı. EN: Company name. Architecture: Transport Property.</summary>
        public string Company { get; init; } = string.Empty;
        /// <summary>TR: Kampanya başlığı. EN: Campaign title. Architecture: Transport Property.</summary>
        public string Title { get; init; } = string.Empty;
        /// <summary>TR: Minimum tutar. EN: Minimum amount. Architecture: Transport Property.</summary>
        public decimal MinimumAmount { get; init; }
        /// <summary>TR: Cashback tutarı. EN: Cashback amount. Architecture: Transport Property.</summary>
        public decimal CashbackAmount { get; init; }
    }

    /// <summary>TR: FakeCampaign katılım transport modelidir. EN: FakeCampaign participation transport model. Architecture: Anti-Corruption Transport Model.</summary>
    private sealed class ExternalParticipationStatus
    {
        /// <summary>TR: Katılım durumu. EN: Participation state. Architecture: Transport Property.</summary>
        public bool IsJoined { get; init; }
    }
}

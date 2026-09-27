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

    /// <summary>
    /// TR: Typed HttpClient bağımlılığını alır.
/// EN: Receives the typed HttpClient dependency.
/// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="httpClient">TR: HTTP istemcisi. EN: HTTP client.</param>
    public CampaignClient(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<CampaignResponse>> GetCampaignsAsync()
    {
        var externalCampaigns = await _httpClient.GetFromJsonAsync<ExternalCampaign[]>("api/campaigns")
            ?? [];

        return externalCampaigns
            .Select(Map)
            .ToArray();
    }

    /// <summary>
    /// TR: Dış servis modelini wallet API response modeline açık kodla dönüştürür.
/// EN: Explicitly maps the external service model to the wallet API response model.
/// Architecture: Explicit Mapper Pattern; AutoMapper intentionally not used.
    /// </summary>
    /// <param name="campaign">TR: Dış kampanya modeli. EN: External campaign model.</param>
    /// <returns>TR: API kampanya modeli. EN: API campaign model.</returns>
    private static CampaignResponse Map(ExternalCampaign campaign) => new()
    {
        Id = campaign.Id,
        Company = campaign.Company,
        Title = campaign.Title,
        MinimumAmount = campaign.MinimumAmount,
        CashbackAmount = campaign.CashbackAmount
    };

    /// <summary>
    /// TR: Yalnız FakeCampaign servisiyle konuşmak için kullanılan iç transport modelidir.
/// EN: Internal transport model used only for communication with FakeCampaign.
/// Architecture: Anti-Corruption Transport Model.
    /// </summary>
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
}

using System.Net.Http.Json;
using RockBreaker.Pay.Modules.Identity.Domain;

namespace RockBreaker.Pay.Modules.Compliance;

/// <summary>
/// TR: FakeKyc REST servisini typed HttpClient ile çağırır ve provider modelini normalize eder.
/// EN: Calls the FakeKyc REST service using a typed HttpClient and normalizes the provider model.
/// Architecture: HTTP Adapter + Anti-Corruption Layer + Explicit Mapper.
/// </summary>
public sealed class KycProviderClient : IKycProviderClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// TR: Typed HttpClient bağımlılığını alır.
    /// EN: Receives the typed HttpClient dependency.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="httpClient">TR: HTTP istemcisi. EN: HTTP client.</param>
    public KycProviderClient(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<KycProviderResult> VerifyAsync(UserAccount user)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/kyc/verifications",
            new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Phone
            });

        response.EnsureSuccessStatusCode();

        var provider = await response.Content.ReadFromJsonAsync<ProviderResponse>()
            ?? throw new InvalidOperationException("FakeKyc returned an empty response.");

        if (provider.Status is not (KycStates.Verified or KycStates.Pending or KycStates.Rejected))
        {
            throw new InvalidOperationException(
                $"FakeKyc returned unsupported status '{provider.Status}'.");
        }

        return new KycProviderResult
        {
            VerificationId = provider.VerificationId,
            Status = provider.Status,
            RiskScore = provider.RiskScore,
            Reason = provider.Reason
        };
    }

    /// <summary>
    /// TR: Yalnız FakeKyc transport cevabını deserialize etmek için kullanılan iç modeldir.
    /// EN: Internal model used only to deserialize the FakeKyc transport response.
    /// Architecture: Transport DTO.
    /// </summary>
    private sealed class ProviderResponse
    {
        /// <summary>TR: Provider doğrulama kimliği. EN: Provider verification identifier. Architecture: Transport Property.</summary>
        public Guid VerificationId { get; init; }

        /// <summary>TR: Provider durumu. EN: Provider status. Architecture: Transport Property.</summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>TR: Provider risk skoru. EN: Provider risk score. Architecture: Transport Property.</summary>
        public int RiskScore { get; init; }

        /// <summary>TR: Provider gerekçesi. EN: Provider reason. Architecture: Transport Property.</summary>
        public string Reason { get; init; } = string.Empty;
    }
}

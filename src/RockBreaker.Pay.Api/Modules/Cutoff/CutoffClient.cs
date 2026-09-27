using System.Net.Http.Json;

namespace RockBreaker.Pay.Modules.Cutoff;

/// <summary>
/// TR: FakeCutoff REST servisine HTTP üzerinden erişir.
/// EN: Accesses the FakeCutoff REST service over HTTP.
/// Architecture: Typed HttpClient + Anti-Corruption Layer.
/// </summary>
public sealed class CutoffClient : ICutoffClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// TR: Typed HttpClient bağımlılığını alır.
    /// EN: Receives the typed HttpClient dependency.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="httpClient">TR: HTTP istemcisi. EN: HTTP client.</param>
    public CutoffClient(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<bool> IsOperationAllowedAsync(string operationType)
    {
        var response = await _httpClient.GetFromJsonAsync<CutoffResponse>(
            $"api/cutoff/check?operationType={Uri.EscapeDataString(operationType)}");

        return response?.Allowed ?? false;
    }

    private sealed class CutoffResponse
    {
        public bool Allowed { get; init; }
    }
}

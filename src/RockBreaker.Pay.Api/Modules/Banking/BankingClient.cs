using System.Net.Http.Json;

namespace RockBreaker.Pay.Modules.Banking;

/// <summary>
/// TR: FakeBanking REST servisine typed HttpClient ile erişir.
/// EN: Accesses the FakeBanking REST service using a typed HttpClient.
/// Architecture: HTTP Adapter + Anti-Corruption Layer.
/// </summary>
public sealed class BankingClient : IBankingClient
{
    private readonly HttpClient _httpClient;

    /// <summary>TR: HTTP client bağımlılığını alır. EN: Receives HTTP client dependency. Architecture: Constructor Injection.</summary>
    public BankingClient(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public Task<bool> TransferToWalletAsync(Guid bankAccountId, Guid walletId, decimal amount) =>
        PostAsync("api/banking/transfers/to-wallet", bankAccountId, walletId, amount, null);

    /// <inheritdoc />
    public Task<bool> TransferFromWalletAsync(Guid bankAccountId, Guid walletId, decimal amount) =>
        PostAsync("api/banking/transfers/from-wallet", bankAccountId, walletId, amount, null);

    /// <inheritdoc />
    public Task<bool> ReverseAsync(Guid bankAccountId, Guid walletId, decimal amount, string originalType) =>
        PostAsync("api/banking/transfers/reverse", bankAccountId, walletId, amount, originalType);

    private async Task<bool> PostAsync(
        string path,
        Guid bankAccountId,
        Guid walletId,
        decimal amount,
        string? originalType)
    {
        using var response = await _httpClient.PostAsJsonAsync(path, new
        {
            BankAccountId = bankAccountId,
            WalletId = walletId,
            Amount = amount,
            OriginalType = originalType
        });

        return response.IsSuccessStatusCode;
    }
}

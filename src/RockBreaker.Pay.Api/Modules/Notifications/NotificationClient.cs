using System.Net.Http.Json;

namespace RockBreaker.Pay.Modules.Notifications;

/// <summary>
/// TR: FakeNotification REST servisine typed HttpClient ile bağlanır.
/// EN: Connects to the FakeNotification REST service using a typed HttpClient.
/// Architecture: HTTP Adapter + Anti-Corruption Layer.
/// </summary>
public sealed class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// TR: HTTP istemcisini alır.
    /// EN: Receives the HTTP client.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="httpClient">TR: Typed HttpClient. EN: Typed HttpClient.</param>
    public NotificationClient(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<bool> SendAsync(string channel, string recipient, string message)
    {
        var path = channel.ToUpperInvariant() switch
        {
            "SMS" => "api/notifications/sms",
            "EMAIL" => "api/notifications/email",
            "PUSH" => "api/notifications/push",
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unsupported notification channel.")
        };

        using var response = await _httpClient.PostAsJsonAsync(path, new
        {
            Recipient = recipient,
            Message = message
        });

        return response.IsSuccessStatusCode;
    }
}

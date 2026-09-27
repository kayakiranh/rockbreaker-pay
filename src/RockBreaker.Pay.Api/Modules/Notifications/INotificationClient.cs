namespace RockBreaker.Pay.Modules.Notifications;

/// <summary>
/// TR: FakeNotification dış servisine SMS, e-mail ve push mesajı gönderimini soyutlar.
/// EN: Abstracts SMS, e-mail and push delivery to the external FakeNotification service.
/// Architecture: Anti-Corruption Layer + Dependency Inversion.
/// </summary>
public interface INotificationClient
{
    /// <summary>
    /// TR: Belirtilen kanaldan mesaj gönderir.
    /// EN: Sends a message through the specified channel.
    /// Architecture: External Command Port.
    /// </summary>
    /// <param name="channel">TR: SMS, EMAIL veya PUSH kanalı. EN: SMS, EMAIL or PUSH channel.</param>
    /// <param name="recipient">TR: Alıcı adresi/telefonu/cihaz kimliği. EN: Recipient address/phone/device identifier.</param>
    /// <param name="message">TR: Bildirim metni. EN: Notification message.</param>
    /// <returns>TR: Dış servisin başarılı olup olmadığı. EN: Whether the external service succeeded.</returns>
    Task<bool> SendAsync(string channel, string recipient, string message);
}

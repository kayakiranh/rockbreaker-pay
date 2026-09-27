using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace RockBreaker.Pay.Modules.Government;

/// <summary>
/// TR: CoreWCF tabanlı FakeGovernment servisine SOAP 1.1 mesajları gönderir.
/// EN: Sends SOAP 1.1 messages to the CoreWCF-based FakeGovernment service.
/// Architecture: SOAP HTTP Adapter + Anti-Corruption Layer.
/// </summary>
public sealed class GovernmentSoapClient : IGovernmentSoapClient
{
    private const string ServiceNamespace = "http://tempuri.org/";
    private readonly HttpClient _httpClient;

    /// <summary>
    /// TR: SOAP HTTP istemcisini alır.
    /// EN: Receives the SOAP HTTP client.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="httpClient">TR: HTTP istemcisi. EN: HTTP client.</param>
    public GovernmentSoapClient(HttpClient httpClient) => _httpClient = httpClient;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<BillResponse>> GetBillsAsync(string citizenNumber)
    {
        var body = $"""
            <GetBills xmlns="{ServiceNamespace}">
              <citizenNumber>{System.Security.SecurityElement.Escape(citizenNumber)}</citizenNumber>
            </GetBills>
            """;

        var xml = await SendAsync("IGovernmentService/GetBills", body);
        XNamespace ns = ServiceNamespace;

        return xml
            .Descendants(ns + "BillDto")
            .Select(element => new BillResponse
            {
                Id = Guid.Parse(element.Element(ns + "Id")?.Value ?? Guid.Empty.ToString()),
                Institution = element.Element(ns + "Institution")?.Value ?? string.Empty,
                Amount = decimal.Parse(
                    element.Element(ns + "Amount")?.Value ?? "0",
                    CultureInfo.InvariantCulture),
                IsPaid = bool.Parse(element.Element(ns + "IsPaid")?.Value ?? "false")
            })
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<bool> PayBillAsync(Guid billId)
    {
        var body = $"""
            <PayBill xmlns="{ServiceNamespace}">
              <billId>{billId}</billId>
            </PayBill>
            """;

        var xml = await SendAsync("IGovernmentService/PayBill", body);
        XNamespace ns = ServiceNamespace;
        return bool.TryParse(
            xml.Descendants(ns + "Success").FirstOrDefault()?.Value,
            out var success) && success;
    }

    /// <summary>
    /// TR: SOAP envelope oluşturur, SOAPAction header'ını ekler ve XML cevabını parse eder.
    /// EN: Creates the SOAP envelope, adds the SOAPAction header and parses the XML response.
    /// Architecture: SOAP Transport Adapter.
    /// </summary>
    /// <param name="action">TR: SOAP action suffix. EN: SOAP action suffix.</param>
    /// <param name="body">TR: SOAP body içeriği. EN: SOAP body content.</param>
    /// <returns>TR: SOAP XML cevabı. EN: SOAP XML response.</returns>
    private async Task<XDocument> SendAsync(string action, string body)
    {
        var envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                {body}
              </soap:Body>
            </soap:Envelope>
            """;

        using var request = new HttpRequestMessage(HttpMethod.Post, "GovernmentService.svc");
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{ServiceNamespace}{action}\"");
        request.Content = new StringContent(envelope, Encoding.UTF8, "text/xml");

        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseXml = await response.Content.ReadAsStringAsync();
        return XDocument.Parse(responseXml);
    }
}

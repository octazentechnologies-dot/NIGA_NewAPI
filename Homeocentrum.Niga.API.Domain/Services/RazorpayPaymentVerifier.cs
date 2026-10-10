using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.API.Domain.Services;

public interface IRazorpayPaymentVerifier
{
    bool Configured { get; }
    bool SignatureValid(string? orderId, string? paymentId, string? signature);
    /// <summary>Paid amount in paise for a Razorpay order, or null when the order cannot be read.</summary>
    Task<long?> AmountPaidPaiseAsync(string orderId, CancellationToken cancellationToken = default);
}

/// <summary>Server-side check of a Razorpay checkout: signature = HMAC-SHA256(order_id|payment_id, key secret).</summary>
public sealed class RazorpayPaymentVerifier : IRazorpayPaymentVerifier
{
    public const string HttpClientName = "Razorpay";
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public RazorpayPaymentVerifier(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    private string KeyId => _config["Razorpay:KeyId"] ?? "";
    private string KeySecret => _config["Razorpay:KeySecret"] ?? "";

    public bool Configured => KeyId.Length > 0 && KeySecret.Length > 0;

    public bool SignatureValid(string? orderId, string? paymentId, string? signature)
    {
        if (!Configured || string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(paymentId) || string.IsNullOrWhiteSpace(signature))
            return false;
        var expected = Convert.ToHexString(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(KeySecret), Encoding.UTF8.GetBytes($"{orderId.Trim()}|{paymentId.Trim()}"))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant()));
    }

    public async Task<long?> AmountPaidPaiseAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (!Configured || string.IsNullOrWhiteSpace(orderId))
            return null;
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.razorpay.com/v1/orders/{Uri.EscapeDataString(orderId.Trim())}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{KeyId}:{KeySecret}")));
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return document.RootElement.TryGetProperty("amount_paid", out var paid) && paid.TryGetInt64(out var value) ? value : null;
    }
}

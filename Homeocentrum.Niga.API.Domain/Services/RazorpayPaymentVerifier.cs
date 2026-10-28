using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.API.Domain.Services;

public sealed class RazorpayPaymentDetails
{
    public string Id { get; init; } = "";
    public string? OrderId { get; init; }
    public string Status { get; init; } = "";
    public string? Method { get; init; }
    public long AmountPaise { get; init; }
    public string? Currency { get; init; }
    public string? Bank { get; init; }
    public string? Wallet { get; init; }
    public string? Vpa { get; init; }
    public string? CardNetwork { get; init; }
    public string? CardLast4 { get; init; }
    public string RawJson { get; init; } = "";

    /// <summary>
    /// Auto-capture checkouts report captured; authorized is treated as paid for ledger
    /// (funds are held / soon captured — matches Checkout SUCCESS for consult).
    /// </summary>
    public bool IsCaptured =>
        string.Equals(Status, "captured", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Status, "authorized", StringComparison.OrdinalIgnoreCase);
}

public interface IRazorpayPaymentVerifier
{
    bool Configured { get; }
    bool SignatureValid(string? orderId, string? paymentId, string? signature);
    /// <summary>Paid amount in paise for a Razorpay order, or null when the order cannot be read.</summary>
    Task<long?> AmountPaidPaiseAsync(string orderId, CancellationToken cancellationToken = default);
    /// <summary>GET /v1/payments/{paymentId} — method, status, amount, and related receipt fields.</summary>
    Task<RazorpayPaymentDetails?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
    /// <summary>GET /v1/orders/{orderId}/payments — payments for a Razorpay order (reconcile).</summary>
    Task<IReadOnlyList<RazorpayPaymentDetails>> ListPaymentsForOrderAsync(string orderId, CancellationToken cancellationToken = default);
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

    public async Task<RazorpayPaymentDetails?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        if (!Configured || string.IsNullOrWhiteSpace(paymentId))
            return null;
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.razorpay.com/v1/payments/{Uri.EscapeDataString(paymentId.Trim())}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{KeyId}:{KeySecret}")));
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        string? cardNetwork = null;
        string? cardLast4 = null;
        if (root.TryGetProperty("card", out var card) && card.ValueKind == JsonValueKind.Object)
        {
            cardNetwork = card.TryGetProperty("network", out var n) ? n.GetString() : null;
            cardLast4 = card.TryGetProperty("last4", out var l) ? l.GetString() : null;
        }

        long amount = 0;
        if (root.TryGetProperty("amount", out var amountNode))
        {
            if (amountNode.ValueKind == JsonValueKind.Number)
                amountNode.TryGetInt64(out amount);
            else
                long.TryParse(amountNode.GetString(), out amount);
        }

        return new RazorpayPaymentDetails
        {
            Id = root.TryGetProperty("id", out var id) ? id.GetString() ?? paymentId.Trim() : paymentId.Trim(),
            OrderId = root.TryGetProperty("order_id", out var oid) ? oid.GetString() : null,
            Status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
            Method = root.TryGetProperty("method", out var m) ? m.GetString() : null,
            AmountPaise = amount,
            Currency = root.TryGetProperty("currency", out var cur) ? cur.GetString() : null,
            Bank = root.TryGetProperty("bank", out var bank) ? bank.GetString() : null,
            Wallet = root.TryGetProperty("wallet", out var wallet) ? wallet.GetString() : null,
            Vpa = root.TryGetProperty("vpa", out var vpa) ? vpa.GetString() : null,
            CardNetwork = cardNetwork,
            CardLast4 = cardLast4,
            RawJson = raw
        };
    }

    public async Task<IReadOnlyList<RazorpayPaymentDetails>> ListPaymentsForOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        if (!Configured || string.IsNullOrWhiteSpace(orderId))
            return Array.Empty<RazorpayPaymentDetails>();

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.razorpay.com/v1/orders/{Uri.EscapeDataString(orderId.Trim())}/payments");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{KeyId}:{KeySecret}")));
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<RazorpayPaymentDetails>();

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(raw);
        if (!document.RootElement.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
            return Array.Empty<RazorpayPaymentDetails>();

        var list = new List<RazorpayPaymentDetails>();
        foreach (var root in items.EnumerateArray())
        {
            string? cardNetwork = null;
            string? cardLast4 = null;
            if (root.TryGetProperty("card", out var card) && card.ValueKind == JsonValueKind.Object)
            {
                cardNetwork = card.TryGetProperty("network", out var n) ? n.GetString() : null;
                cardLast4 = card.TryGetProperty("last4", out var l) ? l.GetString() : null;
            }

            long amount = 0;
            if (root.TryGetProperty("amount", out var amountNode))
            {
                if (amountNode.ValueKind == JsonValueKind.Number)
                    amountNode.TryGetInt64(out amount);
                else
                    long.TryParse(amountNode.GetString(), out amount);
            }

            var id = root.TryGetProperty("id", out var idNode) ? idNode.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(id))
                continue;

            list.Add(new RazorpayPaymentDetails
            {
                Id = id,
                OrderId = root.TryGetProperty("order_id", out var oid) ? oid.GetString() : orderId.Trim(),
                Status = root.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                Method = root.TryGetProperty("method", out var m) ? m.GetString() : null,
                AmountPaise = amount,
                Currency = root.TryGetProperty("currency", out var cur) ? cur.GetString() : null,
                Bank = root.TryGetProperty("bank", out var bank) ? bank.GetString() : null,
                Wallet = root.TryGetProperty("wallet", out var wallet) ? wallet.GetString() : null,
                Vpa = root.TryGetProperty("vpa", out var vpa) ? vpa.GetString() : null,
                CardNetwork = cardNetwork,
                CardLast4 = cardLast4,
                RawJson = root.GetRawText()
            });
        }

        return list;
    }
}

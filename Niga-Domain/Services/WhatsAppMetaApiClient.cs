using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services;

public class WhatsAppMetaApiClient : IWhatsAppMetaApiClient
{
    private const string MessagingProduct = "whatsapp";
    private readonly HttpClient _httpClient;
    private readonly WhatsAppMetaOptions _options;
    private readonly ILogger<WhatsAppMetaApiClient> _logger;

    public WhatsAppMetaApiClient(
        HttpClient httpClient,
        IOptions<WhatsAppMetaOptions> options,
        ILogger<WhatsAppMetaApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? MediaId, string? ErrorMessage)> UploadImageAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(MessagingProduct), "messaging_product");

            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(fileContent, "file", $"image.{GetFileExtension(mimeType)}");

            var requestUri = BuildUri($"{_options.PhoneNumberId}/media");
            using var request = CreateAuthorizedRequest(HttpMethod.Post, requestUri, content);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Meta media upload failed. StatusCode={StatusCode}, Response={Response}",
                    response.StatusCode,
                    responseBody);
                return (false, null, ExtractMetaError(responseBody) ?? $"Media upload failed ({(int)response.StatusCode}).");
            }

            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("id", out var idElement))
            {
                return (true, idElement.GetString(), null);
            }

            return (false, null, "Media upload response did not contain media id.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meta media upload network failure.");
            return (false, null, $"Media upload network failure: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendTextMessageAsync(
        string recipientNumber,
        string messageText,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            messaging_product = MessagingProduct,
            recipient_type = "individual",
            to = recipientNumber,
            type = "text",
            text = new
            {
                preview_url = false,
                body = messageText
            }
        };

        return await SendMessageAsync(payload, cancellationToken);
    }

    public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendImageMessageAsync(
        string recipientNumber,
        string mediaId,
        string caption,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            messaging_product = MessagingProduct,
            recipient_type = "individual",
            to = recipientNumber,
            type = "image",
            image = new
            {
                id = mediaId,
                caption
            }
        };

        return await SendMessageAsync(payload, cancellationToken);
    }

    public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendTemplateMessageAsync(
        string recipientNumber,
        string metaTemplateName,
        string languageCode,
        IReadOnlyList<string>? bodyParameters = null,
        CancellationToken cancellationToken = default)
    {
        object[]? components = null;
        if (bodyParameters != null && bodyParameters.Count > 0)
        {
            components = new object[]
            {
                new
                {
                    type = "body",
                    parameters = bodyParameters.Select(p => new
                    {
                        type = "text",
                        text = p
                    }).ToArray()
                }
            };
        }

        var payload = new
        {
            messaging_product = MessagingProduct,
            recipient_type = "individual",
            to = recipientNumber,
            type = "template",
            template = new
            {
                name = metaTemplateName,
                language = new { code = languageCode },
                components
            }
        };

        return await SendMessageAsync(payload, cancellationToken);
    }

    private async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendMessageAsync(
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var requestUri = BuildUri($"{_options.PhoneNumberId}/messages");
            using var request = CreateAuthorizedRequest(HttpMethod.Post, requestUri, content);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Meta message send failed. StatusCode={StatusCode}, Response={Response}",
                    response.StatusCode,
                    responseBody);
                return (false, null, ExtractMetaError(responseBody) ?? $"Message send failed ({(int)response.StatusCode}).");
            }

            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("messages", out var messagesElement)
                && messagesElement.ValueKind == JsonValueKind.Array
                && messagesElement.GetArrayLength() > 0)
            {
                var firstMessage = messagesElement[0];
                if (firstMessage.TryGetProperty("id", out var idElement))
                {
                    return (true, idElement.GetString(), null);
                }
            }

            return (false, null, "Message send response did not contain message id.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Meta message send network failure.");
            return (false, null, $"Message send network failure: {ex.Message}");
        }
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri, HttpContent content)
    {
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        return request;
    }

    private string BuildUri(string relativePath)
    {
        var version = _options.ApiVersion.Trim().TrimStart('/');
        return $"https://graph.facebook.com/{version}/{relativePath.TrimStart('/')}";
    }

    private static string? ExtractMetaError(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.TryGetProperty("message", out var messageElement))
                {
                    return messageElement.GetString();
                }
            }
        }
        catch
        {
            return responseBody;
        }

        return null;
    }

    private static string GetFileExtension(string mimeType)
    {
        return mimeType switch
        {
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpg"
        };
    }
}

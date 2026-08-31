using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence;

public class IntelligenceGptClient : IIntelligenceGptClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly AudioCaseTakingOptions _audioOptions;
    private readonly IRubricPipelineTelemetry _pipelineTelemetry;
    private readonly ILogger<IntelligenceGptClient> _logger;

    public IntelligenceGptClient(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<AudioCaseTakingOptions> audioOptions,
        IRubricPipelineTelemetry pipelineTelemetry,
        ILogger<IntelligenceGptClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAiOptions = openAiOptions.Value;
        _audioOptions = audioOptions.Value;
        _pipelineTelemetry = pipelineTelemetry;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_openAiOptions.ApiKey);

    public async Task<IntelligenceGptResult<T>> CompleteJsonAsync<T>(
        string systemPrompt,
        string userPrompt,
        string stageName,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            if (!_audioOptions.UseMockWhenNoApiKey)
            {
                return new IntelligenceGptResult<T>
                {
                    Success = false,
                    Error = "OpenAI API key is not configured.",
                };
            }

            return new IntelligenceGptResult<T>
            {
                Success = false,
                Error = $"Mock not implemented for stage {stageName}.",
            };
        }

        var requestBody = new
        {
            model = _openAiOptions.ChatModel,
            temperature = 0.1,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);
        var sw = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();
            _pipelineTelemetry.IncrementLlmCalls();

            if (!response.IsSuccessStatusCode)
            {
                return new IntelligenceGptResult<T>
                {
                    Success = false,
                    RequestJson = requestJson,
                    ResponseJson = responseBody,
                    LatencyMs = (int)sw.ElapsedMilliseconds,
                    Error = responseBody,
                };
            }

            using var doc = JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var promptTokens = doc.RootElement.TryGetProperty("usage", out var usage)
                && usage.TryGetProperty("prompt_tokens", out var pt)
                ? pt.GetInt32()
                : 0;
            var completionTokens = usage.ValueKind != JsonValueKind.Undefined
                && usage.TryGetProperty("completion_tokens", out var ct)
                ? ct.GetInt32()
                : 0;

            var result = JsonSerializer.Deserialize<T>(content ?? "{}", JsonOptions);

            return new IntelligenceGptResult<T>
            {
                Success = result != null,
                Result = result,
                RequestJson = requestJson,
                ResponseJson = responseBody,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                LatencyMs = (int)sw.ElapsedMilliseconds,
                Error = result == null ? "Failed to deserialize GPT JSON response." : null,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _pipelineTelemetry.IncrementLlmCalls();
            _logger.LogError(ex, "Intelligence GPT call failed for stage {StageName}.", stageName);
            return new IntelligenceGptResult<T>
            {
                Success = false,
                RequestJson = requestJson,
                LatencyMs = (int)sw.ElapsedMilliseconds,
                Error = ex.Message,
            };
        }
    }
}

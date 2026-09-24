using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Embeddings;

public class EmbeddingClient : IEmbeddingClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly AudioCaseTakingOptions _audioOptions;
    private readonly IRubricPipelineTelemetry _pipelineTelemetry;
    private readonly ILogger<EmbeddingClient> _logger;

    public EmbeddingClient(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<AudioCaseTakingOptions> audioOptions,
        IRubricPipelineTelemetry pipelineTelemetry,
        ILogger<EmbeddingClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAiOptions = openAiOptions.Value;
        _audioOptions = audioOptions.Value;
        _pipelineTelemetry = pipelineTelemetry;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_openAiOptions.ApiKey);

    public async Task<EmbeddingClientResult> EmbedTextsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default) =>
        await EmbedTextsAsync(texts, _openAiOptions.EmbeddingModel, cancellationToken);

    public async Task<EmbeddingClientResult> EmbedTextsAsync(
        IReadOnlyList<string> texts,
        string modelName,
        CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return new EmbeddingClientResult { Success = true };
        }

        if (!IsConfigured)
        {
            return new EmbeddingClientResult
            {
                Success = false,
                Error = _audioOptions.UseMockWhenNoApiKey
                    ? "Embedding API not configured."
                    : "OpenAI API key is not configured.",
            };
        }

        var requestBody = new
        {
            model = string.IsNullOrWhiteSpace(modelName) ? _openAiOptions.EmbeddingModel : modelName,
            input = texts,
        };

        var sw = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, JsonOptions),
                    Encoding.UTF8,
                    "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();
            _pipelineTelemetry.IncrementEmbeddingCalls();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Embedding API failed: {Status} {Body}", response.StatusCode, responseBody);
                return new EmbeddingClientResult
                {
                    Success = false,
                    Error = responseBody,
                    LatencyMs = (int)sw.ElapsedMilliseconds,
                };
            }

            using var doc = JsonDocument.Parse(responseBody);
            var vectors = doc.RootElement.GetProperty("data")
                .EnumerateArray()
                .OrderBy(x => x.GetProperty("index").GetInt32())
                .Select(x => x.GetProperty("embedding").EnumerateArray()
                    .Select(v => v.GetSingle())
                    .ToArray())
                .ToList();

            return new EmbeddingClientResult
            {
                Success = true,
                Vectors = vectors,
                LatencyMs = (int)sw.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _pipelineTelemetry.IncrementEmbeddingCalls();
            _logger.LogWarning(ex, "Embedding API call failed.");
            return new EmbeddingClientResult
            {
                Success = false,
                Error = ex.Message,
                LatencyMs = (int)sw.ElapsedMilliseconds,
            };
        }
    }
}

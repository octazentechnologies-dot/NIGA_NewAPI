using System;

namespace Niga_Domain.Master;

public partial class AudioCaseAiRequestLog
{
    public long AiRequestLogId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string Provider { get; set; } = null!;

    public string ServiceType { get; set; } = null!;

    public string? ModelName { get; set; }

    public string? RequestId { get; set; }

    public int? HttpStatusCode { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public int? AudioDurationSeconds { get; set; }

    public decimal? EstimatedCostUsd { get; set; }

    public int? LatencyMs { get; set; }

    public string? RequestPayloadHash { get; set; }

    public string? ResponsePayloadHash { get; set; }

    public string? RequestPayloadJson { get; set; }

    public string? ResponsePayloadJson { get; set; }

    public bool IsSuccess { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? CorrelationId { get; set; }

    public DateTime EnteredDate { get; set; }
}

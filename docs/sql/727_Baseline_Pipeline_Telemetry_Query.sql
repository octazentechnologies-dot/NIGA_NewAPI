-- =============================================================================
-- 727_Baseline_Pipeline_Telemetry_Query.sql
-- Stage A: extract baseline latency / call counts from AudioCaseIntelligenceLog
-- and AudioCaseAiRequestLog. No transcript / PHI in these aggregates.
-- =============================================================================

-- 1) Latest PipelineBaselineSummary rows (Stage A instrumentation)
SELECT TOP 50
    l.AudioCaseSessionId,
    l.EnteredDate,
    l.LatencyMs AS TotalLatencyMs,
    l.Message,
    l.DetailsJson
FROM dbo.AudioCaseIntelligenceLog l
WHERE l.StageName = N'PipelineBaselineSummary'
  AND l.EngineVersion = N'base-a'
ORDER BY l.EnteredDate DESC;

-- 2) Stage breakdown for a single session (replace @SessionId)
DECLARE @SessionId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000000'; -- <-- set me

SELECT
    StageName,
    Status,
    LatencyMs,
    Message,
    EnteredDate,
    EngineVersion
FROM dbo.AudioCaseIntelligenceLog
WHERE AudioCaseSessionId = @SessionId
  AND EngineVersion = N'base-a'
ORDER BY EnteredDate;

-- 3) OpenAI call counts / latency for the same session
SELECT
    ServiceType,
    ModelName,
    COUNT(*) AS CallCount,
    AVG(LatencyMs) AS AvgLatencyMs,
    SUM(LatencyMs) AS SumLatencyMs,
    SUM(ISNULL(PromptTokens, 0)) AS PromptTokens,
    SUM(ISNULL(CompletionTokens, 0)) AS CompletionTokens
FROM dbo.AudioCaseAiRequestLog
WHERE AudioCaseSessionId = @SessionId
GROUP BY ServiceType, ModelName
ORDER BY SumLatencyMs DESC;

-- 4) Session wall-clock (EnteredDate → CompletedAtUtc) for recent completed cases
SELECT TOP 20
    s.AudioCaseSessionId,
    s.Status,
    s.EnteredDate,
    s.CompletedAtUtc,
    DATEDIFF(SECOND, s.EnteredDate, s.CompletedAtUtc) AS WallClockSeconds,
    s.ConceptGraphEngineVersion,
    s.RecallEngineVersion
FROM dbo.AudioCaseSession s
WHERE s.DeleteStatus = 0
  AND s.Status = N'Completed'
  AND s.CompletedAtUtc IS NOT NULL
ORDER BY s.CompletedAtUtc DESC;

-- 5) Aggregate stage averages across last N baseline sessions
;WITH BaselineSessions AS (
    SELECT TOP 20 AudioCaseSessionId
    FROM dbo.AudioCaseIntelligenceLog
    WHERE StageName = N'PipelineBaselineSummary'
      AND EngineVersion = N'base-a'
    ORDER BY EnteredDate DESC
)
SELECT
    l.StageName,
    COUNT(*) AS Samples,
    AVG(CAST(l.LatencyMs AS FLOAT)) AS AvgMs,
    MIN(l.LatencyMs) AS MinMs,
    MAX(l.LatencyMs) AS MaxMs
FROM dbo.AudioCaseIntelligenceLog l
INNER JOIN BaselineSessions b ON b.AudioCaseSessionId = l.AudioCaseSessionId
WHERE l.EngineVersion = N'base-a'
  AND l.StageName <> N'PipelineBaselineSummary'
GROUP BY l.StageName
ORDER BY AvgMs DESC;

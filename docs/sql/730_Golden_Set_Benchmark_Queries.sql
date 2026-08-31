/*
  730 — Golden-set / accuracy / performance measurement queries.
  Phases 43–46, 56, 60. READ-ONLY. No DROP/DELETE/TRUNCATE.

  Fill docs/AI_RUBRIC_ENGINE_ACCURACY_BENCHMARK.md and
  docs/AI_RUBRIC_ENGINE_PERFORMANCE_BENCHMARK.md from these results.
  Do NOT invent Precision@10 or doctor-acceptance rates.
*/

SET NOCOUNT ON;
GO

-- A) Candidate golden sessions: completed sessions with doctor feedback
SELECT TOP 200
    s.AudioCaseSessionId,
    s.DoctorUserId,
    s.IntelligenceEngineVersion,
    s.ConceptGraphEngineVersion,
    s.CompletedAtUtc,
    COUNT(DISTINCT f.SubSectionId) AS FeedbackRubricCount,
    SUM(CASE WHEN f.FeedbackType = N'Accepted' THEN 1 ELSE 0 END) AS AcceptedCount,
    SUM(CASE WHEN f.FeedbackType = N'Rejected' THEN 1 ELSE 0 END) AS RejectedCount
FROM dbo.AudioCaseSession s
INNER JOIN dbo.AudioCaseRubricFeedback f
    ON f.AudioCaseSessionId = s.AudioCaseSessionId
WHERE s.DeleteStatus = 0
  AND s.Status = N'Completed'
  AND f.SubSectionId IS NOT NULL
GROUP BY
    s.AudioCaseSessionId,
    s.DoctorUserId,
    s.IntelligenceEngineVersion,
    s.ConceptGraphEngineVersion,
    s.CompletedAtUtc
ORDER BY s.CompletedAtUtc DESC;
GO

-- B) Aggregate doctor acceptance by engine version
SELECT
    ISNULL(NULLIF(LTRIM(RTRIM(s.IntelligenceEngineVersion)), N''), N'(unknown)') AS EngineVersion,
    COUNT(*) AS FeedbackRows,
    SUM(CASE WHEN f.FeedbackType = N'Accepted' THEN 1 ELSE 0 END) AS Accepted,
    SUM(CASE WHEN f.FeedbackType = N'Rejected' THEN 1 ELSE 0 END) AS Rejected,
    CAST(
        SUM(CASE WHEN f.FeedbackType = N'Accepted' THEN 1.0 ELSE 0 END)
        / NULLIF(COUNT(*), 0)
        AS DECIMAL(6,4)
    ) AS AcceptanceRate
FROM dbo.AudioCaseRubricFeedback f
INNER JOIN dbo.AudioCaseSession s
    ON s.AudioCaseSessionId = f.AudioCaseSessionId
WHERE s.DeleteStatus = 0
GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(s.IntelligenceEngineVersion)), N''), N'(unknown)')
ORDER BY FeedbackRows DESC;
GO

-- C) Stage latency averages (fast path stages)
SELECT
    StageName,
    ISNULL(NULLIF(LTRIM(RTRIM(EngineVersion)), N''), N'(unknown)') AS EngineVersion,
    COUNT(*) AS N,
    CAST(AVG(CAST(LatencyMs AS FLOAT)) AS INT) AS AvgMs,
    MIN(LatencyMs) AS MinMs,
    MAX(LatencyMs) AS MaxMs
FROM dbo.AudioCaseIntelligenceLog
WHERE LatencyMs IS NOT NULL
  AND StageName IN (
      N'Whisper',
      N'GptExtraction',
      N'FastClinicalRetrieval',
      N'MatchRubricsWithIntelligence',
      N'ProcessSessionTotal',
      N'PipelineBaselineSummary',
      N'SemanticCacheWait'
  )
GROUP BY StageName, ISNULL(NULLIF(LTRIM(RTRIM(EngineVersion)), N''), N'(unknown)')
ORDER BY StageName, EngineVersion;
GO

-- D) Approximate P50 / P90 via NTILE (compatible with older SQL Server)
;WITH ranked AS (
    SELECT
        StageName,
        EngineVersion,
        LatencyMs,
        NTILE(100) OVER (PARTITION BY StageName, EngineVersion ORDER BY LatencyMs) AS Pctile
    FROM (
        SELECT
            StageName,
            ISNULL(NULLIF(LTRIM(RTRIM(EngineVersion)), N''), N'(unknown)') AS EngineVersion,
            LatencyMs
        FROM dbo.AudioCaseIntelligenceLog
        WHERE LatencyMs IS NOT NULL
          AND StageName IN (
              N'ProcessSessionTotal',
              N'Whisper',
              N'FastClinicalRetrieval',
              N'GptExtraction',
              N'SemanticCacheWait'
          )
    ) s
)
SELECT
    StageName,
    EngineVersion,
    MAX(CASE WHEN Pctile = 50 THEN LatencyMs END) AS ApproxP50Ms,
    MAX(CASE WHEN Pctile = 90 THEN LatencyMs END) AS ApproxP90Ms,
    COUNT(*) AS N
FROM ranked
GROUP BY StageName, EngineVersion
ORDER BY StageName, EngineVersion;
GO

-- E) fast-f doctor accuracy (feeds /AudioCaseIntelligence/benchmark/summary FastF* fields)
SELECT
    COUNT(DISTINCT s.AudioCaseSessionId) AS FastFSessionCount,
    COUNT(*) AS FastFFeedbackCount,
    CAST(SUM(CASE WHEN f.FeedbackType = N'Accepted' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) AS DECIMAL(6,4)) AS FastFAcceptanceRate,
    CAST(SUM(CASE WHEN f.FeedbackType = N'Rejected' THEN 1.0 ELSE 0 END) / NULLIF(COUNT(*), 0) AS DECIMAL(6,4)) AS FastFFalsePositiveRate
FROM dbo.AudioCaseRubricFeedback f
INNER JOIN dbo.AudioCaseSession s
    ON s.AudioCaseSessionId = f.AudioCaseSessionId
WHERE s.DeleteStatus = 0
  AND s.IntelligenceEngineVersion LIKE N'fast%';
GO

-- F) Match-log SubSectionIds that do not resolve in live SubSectionMaster (read-only)
SELECT
    COUNT(*) AS UnresolvedMatchLogRows,
    COUNT(DISTINCT m.AudioCaseSessionId) AS SessionsAffected
FROM dbo.AudioCaseRubricMatchLog m
INNER JOIN dbo.AudioCaseSession s
    ON s.AudioCaseSessionId = m.AudioCaseSessionId
WHERE s.DeleteStatus = 0
  AND s.Status = N'Completed'
  AND m.SubSectionId > 0
  AND NOT EXISTS (
        SELECT 1
        FROM dbo.SubSectionMaster sm
        WHERE sm.SubSectionId = m.SubSectionId
          AND sm.DeleteStatus = 0
  );
GO

PRINT '730 benchmark queries completed (read-only). Copy results into accuracy/performance docs.';
GO

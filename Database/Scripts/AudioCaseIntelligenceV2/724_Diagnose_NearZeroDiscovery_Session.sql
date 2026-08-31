-- Diagnostic queries for RegressionCase_NearZeroDiscovery near-zero rubric failure.
-- Safe to run as-is: auto-picks the most recent matching session into @SessionId.
-- NOTE: This is NOT the gold-case seed. Seed script is 723_Seed_GoldCase_RegressionCase_NearZeroDiscovery.sql

DECLARE @SessionId UNIQUEIDENTIFIER;
DECLARE @SessionIdText NVARCHAR(36) = NULL;

-- Paste YOUR latest session id from the API/UI (recommended after each fresh run).
-- Must be 8-4-4-4-12 hex digits. Common typo: 7fa63c9-... (invalid) vs 87fa63c9-... (valid).
-- Leave NULL only if you want auto-pick (see note below — it may NOT be your newest run).
--
-- CORRECTNESS GATE (Bugs A–D) — verify BEFORE enabling doctor panel:
--   A) Zero DROPSY / substring-collision hits for "drop(s)" concepts
--   B) Each rubric MatchedFrom / citation equals its own concept (no shared "Memory Loss" labels)
--   C) Zero BLADDER/ABDOMEN "...thirst, with" hitchhikers outranking STOMACH-THIRST
--   D) No concept appears in both Section A (DB) and Section B (AI Clinical Concept)
-- LATENCY GAPS (Parts 1–3) — also verify on fresh gold run:
--   Gap1: WorkerDequeued → ProcessSessionStarted / SemanticCacheWait* (target: seconds, not ~10 min)
--   Gap2: ConceptDiscoveryCoverage.perConcept[].LatencyMs (target: parallel, not 9× sequential minutes)
--   Gap3: LatencyGap3_SaveDisplayedRubrics / StatusCompletedSaved (target: seconds, not ~5 min)
-- Only after A–D + gaps pass on 2–3 gold cases: check gold recall ≥5/7 and elapsed <3 min.
SET @SessionIdText = NULL;  -- e.g. N'87fa63c9-c270-45c2-bf66-df7c97017404'

-- Recent sessions (run this block to find the id you just created)
SELECT TOP 10
    AudioCaseSessionId,
    Status,
    ConceptGraphEngineVersion,
    IntelligenceEngineVersion,
    DATEDIFF(MINUTE, EnteredDate, ISNULL(ChangedDate, SYSUTCDATETIME())) AS ElapsedMinutes,
    EnteredDate
FROM dbo.AudioCaseSession
WHERE DeleteStatus = 0
ORDER BY EnteredDate DESC;

IF @SessionIdText IS NOT NULL AND LTRIM(RTRIM(@SessionIdText)) <> N''
BEGIN
    SET @SessionId = TRY_CONVERT(UNIQUEIDENTIFIER, LTRIM(RTRIM(@SessionIdText)));
    IF @SessionId IS NULL
    BEGIN
        SELECT N'Invalid SessionId — not a valid GUID. Expected format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx (32 hex digits).'
            + N' You pasted: ' + @SessionIdText AS ErrorMessage;
        RETURN;
    END
END

IF @SessionId IS NULL
BEGIN
    -- Auto-pick: newest gold-case-like session only (NOT necessarily your latest upload).
    SELECT TOP (1) @SessionId = AudioCaseSessionId
    FROM dbo.AudioCaseSession
    WHERE DeleteStatus = 0
      AND (
            TranscriptRaw LIKE N'%scared of the feet%'
         OR TranscriptRaw LIKE N'%I feel scared of the feet%'
         OR SummaryJson LIKE N'%Epileptic fits with premonitory%'
         OR SuggestedRubricsJson LIKE N'%Fear Before Convulsion%'
      )
    ORDER BY EnteredDate DESC;
END

-- A) Session overview + elapsed minutes
SELECT
    AudioCaseSessionId,
    Status,
    CurrentStep,
    ConceptGraphEngineVersion,
    IntelligenceEngineVersion,
    EnteredDate,
    ChangedDate,
    DATEDIFF(MINUTE, EnteredDate, ISNULL(ChangedDate, SYSUTCDATETIME())) AS ElapsedMinutes,
    LEN(ExtractedSymptomsJson) AS SympJsonLen,
    LEN(SuggestedRubricsJson) AS RubricsJsonLen,
    LEFT(TranscriptRaw, 160) AS Preview
FROM dbo.AudioCaseSession
WHERE AudioCaseSessionId = @SessionId;

IF @SessionId IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.AudioCaseSession WHERE AudioCaseSessionId = @SessionId)
BEGIN
    SELECT N'No matching AudioCaseSession found. Paste a SessionId into SET @SessionId above and re-run.' AS ErrorMessage;
    RETURN;
END

SELECT @SessionId AS SelectedSessionId;

-- B) Extraction symptom count
SELECT
    COUNT(*) AS SymptomCount,
    SUM(CASE WHEN JSON_VALUE(s.value, '$.phrase') LIKE N'%fear%' THEN 1 ELSE 0 END) AS FearHits,
    SUM(CASE WHEN JSON_VALUE(s.value, '$.phrase') LIKE N'%sleep%' THEN 1 ELSE 0 END) AS SleepHits,
    SUM(CASE WHEN JSON_VALUE(s.value, '$.phrase') LIKE N'%thirst%'
              OR JSON_VALUE(s.value, '$.phrase') LIKE N'%water%' THEN 1 ELSE 0 END) AS ThirstHits,
    SUM(CASE WHEN JSON_VALUE(s.value, '$.phrase') LIKE N'%sex%'
              OR JSON_VALUE(s.value, '$.phrase') LIKE N'%desire%' THEN 1 ELSE 0 END) AS DesireHits,
    SUM(CASE WHEN JSON_VALUE(s.value, '$.phrase') LIKE N'%drop%'
              OR JSON_VALUE(s.value, '$.phrase') LIKE N'%awkward%' THEN 1 ELSE 0 END) AS DropHits,
    SUM(CASE WHEN JSON_VALUE(s.value, '$.phrase') LIKE N'%salt%' THEN 1 ELSE 0 END) AS SaltHits,
    SUM(CASE WHEN JSON_VALUE(s.value, '$.phrase') LIKE N'%anger%'
              OR JSON_VALUE(s.value, '$.phrase') LIKE N'%vibrat%' THEN 1 ELSE 0 END) AS AngerAuraHits
FROM dbo.AudioCaseSession x
CROSS APPLY OPENJSON(ISNULL(x.ExtractedSymptomsJson, N'[]')) s
WHERE x.AudioCaseSessionId = @SessionId;

-- C) Concept graph node counts
SELECT 'AIPatientMeaning' AS T, COUNT(*) AS C
FROM dbo.AIPatientMeaning WHERE AudioCaseSessionId = @SessionId
UNION ALL
SELECT 'AIHomeopathicConcept', COUNT(*)
FROM dbo.AIHomeopathicConcept WHERE AudioCaseSessionId = @SessionId
UNION ALL
SELECT 'AIRubricDiscovery', COUNT(*)
FROM dbo.AIRubricDiscovery WHERE AudioCaseSessionId = @SessionId
UNION ALL
SELECT 'AIRubricEvidenceComplete', COUNT(*)
FROM dbo.AIRubricEvidence e
INNER JOIN dbo.AIRubricDiscovery d ON d.RubricDiscoveryId = e.RubricDiscoveryId
WHERE d.AudioCaseSessionId = @SessionId AND e.IsComplete = 1;

-- D) Discovery / intelligence stage logs
-- PerConceptKeywordDiscovery DetailsJson = per-concept traces (only present after latest deploy).
-- EnterpriseClinicalValidation DetailsJson = per-candidate RejectionReason (Bug 1).
SELECT StageName, Status, Message, LatencyMs, DetailsJson, EnteredDate
FROM dbo.AudioCaseIntelligenceLog
WHERE AudioCaseSessionId = @SessionId
  AND StageName IN (
        N'DiscoveryPathSelection',
        N'RubricMatchingStarted',
        N'RubricMatchingCompleted',
        N'PerConceptKeywordDiscovery',
        N'HybridEmbeddingSearch',
        N'CaseUnderstanding',
        N'EnterpriseClinicalValidation',
        N'ClinicalValidationV21')
ORDER BY EnteredDate;

-- All intelligence stages with latency (find the time sink)
SELECT StageName, Status, Message, LatencyMs, EnteredDate
FROM dbo.AudioCaseIntelligenceLog
WHERE AudioCaseSessionId = @SessionId
ORDER BY EnteredDate;

SELECT PipelineStage, ModelId, Success, ErrorMessage, LatencyMs, LEFT(RequestJson, 800) AS RequestPreview, EnteredDate
FROM dbo.AIReasoningAudit
WHERE AudioCaseSessionId = @SessionId
ORDER BY EnteredDate;

-- D2) Task 0 latency: AudioCaseAiRequestLog timing breakdown
-- Correct columns: ModelName (not Model), IsSuccess (not Success)
SELECT
    Provider,
    ServiceType,
    ModelName,
    LatencyMs,
    IsSuccess,
    PromptTokens,
    CompletionTokens,
    LEFT(ISNULL(ErrorMessage, N''), 200) AS ErrorPreview,
    EnteredDate
FROM dbo.AudioCaseAiRequestLog
WHERE AudioCaseSessionId = @SessionId
ORDER BY EnteredDate;

-- Sum latency by service (where the 21 minutes went)
SELECT
    ServiceType,
    COUNT(*) AS CallCount,
    SUM(LatencyMs) AS TotalLatencyMs,
    CAST(SUM(LatencyMs) / 60000.0 AS DECIMAL(10, 2)) AS TotalMinutes,
    MAX(LatencyMs) AS MaxLatencyMs,
    SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessCount,
    SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS FailCount
FROM dbo.AudioCaseAiRequestLog
WHERE AudioCaseSessionId = @SessionId
GROUP BY ServiceType
ORDER BY SUM(LatencyMs) DESC;

-- E) Confirm expected rubrics exist in SubSectionMaster
SELECT SubSectionId, SubSectionName
FROM dbo.SubSectionMaster
WHERE DeleteStatus = 0
  AND (
        SubSectionName LIKE N'%drops things%'
     OR SubSectionName LIKE N'%TALKING%sleep%'
     OR SubSectionName LIKE N'%THIRST%large%'
     OR SubSectionName LIKE N'%salt%desire%'
     OR SubSectionName LIKE N'%FEAR%high places%'
     OR SubSectionName LIKE N'%SEXUAL DESIRE%increased%'
     OR SubSectionName LIKE N'%CONVULSIONS%anger%'
  );

-- F) Suggested rubrics vs gold
SELECT
    JSON_VALUE(r.value, '$.subSectionName') AS SuggestedRubric,
    JSON_VALUE(r.value, '$.matchSource') AS MatchSource,
    JSON_VALUE(r.value, '$.matchScore') AS MatchScore,
    JSON_VALUE(r.value, '$.modalityVariantCount') AS ModalityVariantCount
FROM dbo.AudioCaseSession x
CROSS APPLY OPENJSON(ISNULL(x.SuggestedRubricsJson, N'[]')) r
WHERE x.AudioCaseSessionId = @SessionId;

-- G) ConceptDiscoveryCoverage audit (conceptsSearched + per-concept breakdown)
SELECT
    PipelineStage,
    ModelId,
    Success,
    ErrorMessage,
    EnteredDate,
    RequestJson
FROM dbo.AIReasoningAudit
WHERE AudioCaseSessionId = @SessionId
  AND PipelineStage = N'ConceptDiscoveryCoverage';

-- G2) Per-concept keyword traces (ConceptGraphOnly supplement or V2 path)
SELECT
    StageName,
    Status,
    Message,
    LatencyMs,
    DetailsJson,
    EnteredDate
FROM dbo.AudioCaseIntelligenceLog
WHERE AudioCaseSessionId = @SessionId
  AND StageName = N'PerConceptKeywordDiscovery'
ORDER BY EnteredDate DESC;

-- G3) Target concepts: search attempted / raw / kept (from PerConceptKeywordDiscovery DetailsJson)
DECLARE @KeywordDetails NVARCHAR(MAX) = (
    SELECT TOP 1 DetailsJson
    FROM dbo.AudioCaseIntelligenceLog
    WHERE AudioCaseSessionId = @SessionId
      AND StageName = N'PerConceptKeywordDiscovery'
    ORDER BY EnteredDate DESC
);

IF @KeywordDetails IS NOT NULL
BEGIN
    SELECT
        JSON_VALUE(t.value, '$.conceptText') AS ConceptText,
        JSON_VALUE(t.value, '$.category') AS Category,
        JSON_VALUE(t.value, '$.isSrp') AS IsSrp,
        JSON_VALUE(t.value, '$.searchAttempted') AS SearchAttempted,
        JSON_VALUE(t.value, '$.rawCandidateCount') AS RawCandidateCount,
        JSON_VALUE(t.value, '$.keptCandidateCount') AS KeptCandidateCount,
        JSON_VALUE(t.value, '$.outcome') AS Outcome,
        JSON_VALUE(t.value, '$.topRubricName') AS TopRubricName,
        JSON_VALUE(t.value, '$.error') AS Error
    FROM OPENJSON(@KeywordDetails, '$.traces') t
    WHERE JSON_VALUE(t.value, '$.conceptText') LIKE N'%fear%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%height%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%drop%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%sex%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%thirst%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%salt%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%sleep%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%talk%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%vibrat%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%aura%'
       OR JSON_VALUE(t.value, '$.conceptText') LIKE N'%mutton%'
    ORDER BY ConceptText;
END
ELSE
    SELECT N'No PerConceptKeywordDiscovery DetailsJson for this session (pre-Phase-4 deploy or ConceptGraphOnly supplement not run).' AS Note;

-- H) EnterpriseClinicalValidation accept/reject + rejection reasons for target concepts
DECLARE @ValidationDetails NVARCHAR(MAX) = (
    SELECT TOP 1 DetailsJson
    FROM dbo.AudioCaseIntelligenceLog
    WHERE AudioCaseSessionId = @SessionId
      AND StageName = N'EnterpriseClinicalValidation'
    ORDER BY EnteredDate DESC
);

SELECT
    StageName,
    Status,
    Message,
    LEFT(DetailsJson, 4000) AS DetailsPreview,
    EnteredDate
FROM dbo.AudioCaseIntelligenceLog
WHERE AudioCaseSessionId = @SessionId
  AND StageName = N'EnterpriseClinicalValidation'
ORDER BY EnteredDate DESC;

IF @ValidationDetails IS NOT NULL
BEGIN
    SELECT
        JSON_VALUE(r.value, '$.subSectionName') AS RubricName,
        JSON_VALUE(r.value, '$.matchSource') AS MatchSource,
        JSON_VALUE(r.value, '$.accepted') AS Accepted,
        JSON_VALUE(r.value, '$.rejectionReason') AS RejectionReason,
        JSON_VALUE(r.value, '$.matchedFrom') AS MatchedFrom
    FROM OPENJSON(@ValidationDetails, '$.rejected') r
    WHERE JSON_VALUE(r.value, '$.subSectionName') LIKE N'%fear%'
       OR JSON_VALUE(r.value, '$.subSectionName') LIKE N'%height%'
       OR JSON_VALUE(r.value, '$.subSectionName') LIKE N'%drop%'
       OR JSON_VALUE(r.value, '$.subSectionName') LIKE N'%sex%'
       OR JSON_VALUE(r.value, '$.subSectionName') LIKE N'%thirst%'
       OR JSON_VALUE(r.value, '$.subSectionName') LIKE N'%salt%'
       OR JSON_VALUE(r.value, '$.subSectionName') LIKE N'%sleep%'
       OR JSON_VALUE(r.value, '$.subSectionName') LIKE N'%talk%'
       OR JSON_VALUE(r.value, '$.matchedFrom') LIKE N'%fear%'
       OR JSON_VALUE(r.value, '$.matchedFrom') LIKE N'%drop%'
       OR JSON_VALUE(r.value, '$.matchedFrom') LIKE N'%sex%'
       OR JSON_VALUE(r.value, '$.matchedFrom') LIKE N'%thirst%'
       OR JSON_VALUE(r.value, '$.matchedFrom') LIKE N'%salt%'
       OR JSON_VALUE(r.value, '$.matchedFrom') LIKE N'%sleep%'
    ORDER BY RubricName;
END

-- I) Gold rubric hit count (7 expected)
SELECT
    g.ExpectedRubric,
    CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.AudioCaseSession x
        CROSS APPLY OPENJSON(ISNULL(x.SuggestedRubricsJson, N'[]')) r
        WHERE x.AudioCaseSessionId = @SessionId
          AND JSON_VALUE(r.value, '$.subSectionName') LIKE g.Pattern
    ) THEN 1 ELSE 0 END AS Hit
FROM (VALUES
    (N'MIND - FEAR - high places, of', N'%FEAR%high places%'),
    (N'MIND - TALKING - sleep, in', N'%TALKING%sleep%'),
    (N'STOMACH - THIRST - large quantities; for', N'%THIRST%large%'),
    (N'MALE GENITALIA/SEX - SEXUAL DESIRE - increased', N'%SEXUAL DESIRE%increased%'),
    (N'MIND - AWKWARD - drops things', N'%drops things%'),
    (N'GENERALS - CONVULSIONS - anger; after', N'%CONVULSIONS%anger%'),
    (N'GENERALS - FOOD AND DRINKS - salt - desire', N'%salt%desire%')
) g(ExpectedRubric, Pattern);
GO

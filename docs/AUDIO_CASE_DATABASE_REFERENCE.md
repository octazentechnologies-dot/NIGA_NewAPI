# Audio Case Database Reference

Database name in scripts: `HomeoCentrum_Production`.  
EF context: `Niga-Domain/Data/NIGACentrumContext.cs`.  
Migrations folder exists but audio tables are created via **manual SQL scripts**, not EF migrations.

---

## Script map

| Script | Purpose |
| ------ | ------- |
| `Database/Scripts/AudioCaseTaking_CreateTables.sql` | Core session, logs, consent, match, doctor action, retention |
| `Database/Scripts/AudioCaseIntelligenceV2/001–010, 101–103` | Concepts, intelligence log, embeddings, feedback, benchmark, V2 columns |
| `721–722` | Unified scores on match log |
| `728` | Widen `EngineVersion` on intelligence log |
| `725_FullText_SubSectionMaster_SubSectionName.sql` | FTS used by hotspot search (referenced in `SubSectionRepository`) |
| `Database/Scripts/AIV4EmbeddingInfrastructure/801–802` | `AiRubricEmbedding`, `AiConceptEmbedding`, sync state |
| `docs/sql/728_Widen_AudioCaseIntelligenceLog_EngineVersion.sql` | Duplicate/docs copy of 728 |

---

## Audio Case tables

### AudioCaseSession

**Purpose:** One consultation audio analysis.  
**PK:** `AudioCaseSessionId` UNIQUEIDENTIFIER  
**FK:** none declared to Patient in create script (PatientId/CaseId/DoctorUserId stored as BIGINT).  
**Important columns:** `AudioFilePath`, `TranscriptRaw`, `ConversationJson`, `SummaryJson`, `ExtractedSymptomsJson`, `SuggestedRubricsJson`, `ClinicalConceptsJson`, `CausationLinksJson`, `Status`, `CurrentStep`, `DetectedLanguage`, `LanguageOverride`, `CorrelationId`, `ErrorCode`, `ErrorMessage`, `IntelligenceEngineVersion`, `ConceptGraphEngineVersion`, `RecallEngineVersion`, `TranscriptCoverageScore`, `CaseCompletenessScore`, `ReAnalysisCount`, `CompletedAtUtc`, `AudioPurgedAtUtc`, `DeleteStatus`.  
**Indexes:** DoctorUserId+EnteredDate; PatientId+CaseId+EnteredDate.  
**Writes:** upload, process, reanalyze, fail, retention.  
**Reads:** status, result, latest, sessions, download.

### AudioCaseSessionEventLog

**PK:** `EventLogId`  
**FK:** `AudioCaseSessionId` → AudioCaseSession  
**Purpose:** Step audit (`SessionCreated`, `TranscriptionCompleted`, `LatencyGap*`, `SessionFailed`, …).

### AudioCaseAiRequestLog

**PK:** `AiRequestLogId`  
**FK:** session  
**Purpose:** Whisper/GPT payloads, tokens, latency, success. May contain transcript-adjacent JSON — treat as PHI.

### AudioCaseConsentLog

Consent type `AudioRecordingClinical`, `ConsentTextVersion` from config (`v1.0-2026-06-23`).

### AudioCaseRubricMatchLog

Per suggested rubric: scores, rank, match source, explainability JSON, unified scores (`FinalHybridScore`, `EvidenceChainComplete`, `GroundedInOntology`).  
**FK:** session. `SubSectionId` is INT but create script does **not** FK to SubSectionMaster (AI-suggested ids may be 0).

### AudioCaseDoctorActionLog

Accept/reject/apply audit from UI `doctor-action`.

### AudioCaseRetentionLog

File purge actions when retention &gt; 0 (currently disabled).

### AudioCaseClinicalConcept

V2+ persisted concepts for a session (`001_Create_AudioCaseClinicalConcept.sql`). Fast path also saves via `IAudioCaseIntelligenceRepository.SaveConceptsAsync`.

### AudioCaseIntelligenceLog

Stage logs including `FastClinicalRetrieval` (`EngineVersion` widened in 728; telemetry also uses `base-a` constant).

### AudioCaseCausationLink / AudioCaseClinicalInferenceLog

V2 causation/inference. Fast path passes empty causation list.

### AudioCaseRubricFeedback

Doctor Approved / Rejected / Corrected. Feeds learning + benchmark.

### AudioCaseRubricBenchmark

Daily/engine snapshots (`010_Create_AudioCaseRubricBenchmark.sql`).

---

## Repertory / rubric tables (discovery)

### SectionMaster

**PK:** `SectionId`  
**Columns:** `SectionName`, `SectionAlias`, `DeleteStatus`  
**Children:** `SubSectionMaster`

### SubSectionMaster

**PK:** `SubSectionId`  
**FK:** `SectionId` → SectionMaster; `ParentSubSectionId` self  
**Search column:** `SubSectionName` (FTS CONTAINS + `Contains` fallback)  
**Also:** `SubSectionNameAlias`, `Description`, `DeleteStatus`  
**Used by:** V1 hotspot, keyword discovery, catalog, reconciler, embeddings FK.

### RubricRemedyDetail

**PK:** `RubricRemedyId`  
**FK:** `SubSectionId`, `RemedyId`, `GradeId`  
**Purpose:** Remedy mapping / counts (`DeletedStatus`).  
Not queried on the fast discovery hot path except reconciler `RemedyCount`.

### RemedyRubricAuthorDetail

Child of `RubricRemedyDetail`. Not on audio hot path.

### RemedyMaster / RemedyGradeMaster

Remedy lookup after doctor applies rubric to repertorization (existing case-taking, not audio-specific).

---

## Embedding tables

### RubricEmbeddings (legacy)

**PK:** `Id`  
**FK:** `RubricId` → SubSectionMaster  
**Columns:** `EmbeddingJson` (JSON float array, not native VECTOR), `ModelName`, `TextHash`, `SourceType`  
**Config:** `KeepLegacyRubricEmbeddingsActive: true`

### AiRubricEmbedding (V4 infra)

**PK:** `RubricEmbeddingId`  
**FK:** `EmbeddingVersionId` → AiEmbeddingVersion; `RubricId`  
**Columns:** `SourceText`, `TextHash`, `EmbeddingPayloadJson`, `DimensionCount` (1536), `Status`, `RevisionNo`

### AiConceptEmbedding

Concept-level vectors for enterprise/V3 engines. Fast path embedding search uses rubric cache (`IRubricEmbeddingMemoryCache`), not necessarily this table directly.

### AiEmbeddingVersion / AiEmbeddingSyncState / jobs

Builder + incremental refresh (`AiEmbeddingInfrastructure`).

---

## Which tables participate in which concern

| Concern | Tables |
| ------- | ------ |
| Rubric discovery | SubSectionMaster, (FTS index), RubricEmbeddings / AiRubricEmbedding, alias/metaphor admin tables if present |
| Rubric validation | In-process; match log stores outcomes |
| Embedding | RubricEmbeddings, AiRubricEmbedding, AiConceptEmbedding, AiEmbeddingVersion |
| Remedy mapping | RubricRemedyDetail, RemedyMaster (after apply) |
| Doctor review | AudioCaseDoctorActionLog, AudioCaseRubricFeedback, AudioCaseRubricBenchmark |

---

## Relationships

```text
SectionMaster
    └── SubSectionMaster
            ├── RubricRemedyDetail → RemedyMaster
            │         └── RemedyRubricAuthorDetail
            ├── RubricEmbeddings (legacy)
            └── AiRubricEmbedding → AiEmbeddingVersion

AudioCaseSession
    ├── AudioCaseSessionEventLog
    ├── AudioCaseAiRequestLog
    ├── AudioCaseConsentLog
    ├── AudioCaseRubricMatchLog  (SubSectionId logical → SubSectionMaster)
    ├── AudioCaseDoctorActionLog
    ├── AudioCaseRetentionLog
    ├── AudioCaseClinicalConcept
    ├── AudioCaseIntelligenceLog
    ├── AudioCaseCausationLink
    ├── AudioCaseClinicalInferenceLog
    └── AudioCaseRubricFeedback
```

No separate Transcript, AudioCase, or DoctorReview header tables — transcript lives on the session row; review is action + feedback logs.

---

## Persistence writes (audio processing)

| Table | Operation | Trigger | Source |
| ----- | --------- | ------- | ------ |
| AudioCaseSession | INSERT | Upload | service |
| AudioCaseSession | UPDATE | each progress step, complete, fail | service |
| AudioCaseConsentLog | INSERT | Upload | consent true |
| AudioCaseSessionEventLog | INSERT | many steps | LogEventAsync |
| AudioCaseAiRequestLog | INSERT | Whisper/GPT | LogAiRequestAsync |
| AudioCaseClinicalConcept | UPSERT/insert | Fast retrieval | SaveConceptsAsync |
| AudioCaseIntelligenceLog | INSERT | FastClinicalRetrieval stage | repository |
| AudioCaseRubricMatchLog | DELETE+INSERT | PersistRubricMatchLogsAsync | final rubrics |
| AudioCaseSession.SuggestedRubricsJson | UPDATE | finalize | JSON serialize |
| AudioCaseDoctorActionLog | INSERT | doctor-action API | UI |
| AudioCaseRubricFeedback | INSERT | feedback API | UI |
| AudioCaseRetentionLog | INSERT | purge job | if retention enabled |

---

## Sample debug queries (no PHI in comments)

```sql
-- Session timeline
SELECT EventType, EventStatus, Message, EnteredDate, DurationMs
FROM dbo.AudioCaseSessionEventLog
WHERE AudioCaseSessionId = @id
ORDER BY EventLogId;

-- AI calls
SELECT ServiceType, ModelName, LatencyMs, IsSuccess, PromptTokens, CompletionTokens, ErrorMessage, EnteredDate
FROM dbo.AudioCaseAiRequestLog
WHERE AudioCaseSessionId = @id
ORDER BY AiRequestLogId;

-- Suggested rubrics
SELECT RankPosition, SubSectionId, SubSectionName, FinalScore, MatchSource, IsSelectedForUi
FROM dbo.AudioCaseRubricMatchLog
WHERE AudioCaseSessionId = @id
ORDER BY RankPosition;
```

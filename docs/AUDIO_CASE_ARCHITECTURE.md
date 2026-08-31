# Audio Case Architecture

**Source of truth:** `NigaHomeopathy-API` + `NigaHomeopathy-UI` source code as of 2026-08-16.  
**Production engine:** `fast-f` via `RubricIntelligence:EnableFastClinicalRetrievalPipeline = true`.  
**Confidence:** CONFIRMED unless marked otherwise.

Related files: [complete documentation](./AUDIO_CASE_AI_RUBRIC_COMPLETE_DOCUMENTATION.md) · [rubric engine](./AUDIO_CASE_RUBRIC_ENGINE.md) · [API](./AUDIO_CASE_API_REFERENCE.md)

---

## Repositories in this workspace

| Repo | Role for Audio Case | Status |
| ---- | ------------------- | ------ |
| `NigaHomeopathy-API` | Current backend (ASP.NET Core 8, Niga-Web + Niga-Domain) | CONFIRMED current |
| `NigaHomeopathy-UI` | Current frontend (React 18 + Redux Toolkit + react-scripts) | CONFIRMED current |
| `NIGA_Latest_Code_API` | ASP.NET Core 2.2 `NIGA.Centrum` repertory-admin API (`api/subsection` FTS `CONTAINSTABLE` on `SearchNormalized`) | No Whisper / OpenAI / Audio Case; classic rubric CRUD only |
| `minimal` | Velzon React template workspace | No Audio Case components found |

Older `AI_RUBRIC_ENGINE_*.md` files in this folder describe earlier engines (V2–V7). Where they disagree with `appsettings.json` + `AudioCaseTakingService.MatchRubricsWithIntelligenceAsync`, **the source code wins**.

---

## Current production path

```text
Doctor UI (AudioCasePanel)
    ↓ JWT Bearer
POST /api/AudioCaseTaking/upload
    ↓ disk store + AudioCaseSession row
In-memory Channel queue (AudioCaseTakingQueue)
    ↓ AudioCaseTakingBackgroundService (single reader)
ProcessSessionAsync
    ↓
Whisper translations (whisper-1)  →  TranscriptRaw
    ↓
GPT-4o ExtractCaseDataAsync       →  ConversationJson / SummaryJson / ExtractedSymptomsJson
    ↓
FastClinicalRetrievalOrchestrator (engine fast-f)
    parallel: V1 hotspot/FTS + Keyword + Alias + Embedding + Catalog
    ↓ gates + CanonicalScore + MMR
SuggestedRubricsJson + AudioCaseRubricMatchLog
    ↓
GET /status (poll 2.5s) → GET /result
    ↓
Doctor Approve / Reject / Add to repertorization
    ↓
POST /doctor-action + POST /rubrics/feedback
```

Rollback: set `EnableFastClinicalRetrievalPipeline` to `false`. That re-enables the V3/V7/Enterprise path. See [version history](./AUDIO_CASE_VERSION_HISTORY.md).

---

## Layer diagram

```text
NigaHomeopathy-UI
  AudioCasePanel / useAudioRecorder / Redux thunks
        │  axios (JWT from sessionStorage.authUser)
        ▼
Niga-Web  AudioCaseTakingController  [Authorize]
        │
        ▼
Niga-Domain
  AudioCaseTakingService          session lifecycle
  AudioCaseAiProcessor            Whisper + GPT extraction + optional AI rubric names
  FastClinicalRetrievalOrchestrator   production rubric discovery
  ClinicalValidationEngine        enterprise 8-step validation (still applied after fast path)
  AiSuggestedRubricReconciler     map AI names → SubSectionMaster
        │
        ▼
SQL Server  HomeoCentrum_Production
  AudioCase* tables, SubSectionMaster, RubricEmbeddings / AiRubricEmbedding
        │
        ▼
OpenAI  https://api.openai.com/v1
  audio/translations | audio/transcriptions | chat/completions | embeddings
```

---

## What is NOT on the production hot path

When `EnableFastClinicalRetrievalPipeline` is true, these still exist in the repo but are **not called** by `MatchRubricsWithIntelligenceAsync`:

| Component | Path | Status |
| --------- | ---- | ------ |
| V3 Concept Graph (M0–M5) | `Services/AudioCaseIntelligence/V3` | LEGACY / rollback |
| V6 Clinical Reasoning | `Services/AudioCaseIntelligence/V6` | Disabled (`EnableV6ClinicalReasoningEngine: false`) |
| V7 Repertory Intelligence | `Services/AudioCaseIntelligence/RepertoryIntelligence` | LEGACY / rollback |
| ECI v8 | `Services/AudioCaseIntelligence/ECI/V8` | Disabled (`EnableEciV8Engine` default false) |
| V2 `RubricIntelligenceOrchestrator` | `Orchestration/RubricIntelligenceOrchestrator.cs` | LEGACY / rollback |

CONFIRMED: `AudioCaseTakingService.MatchRubricsWithIntelligenceAsync` returns immediately into `MatchRubricsFastClinicalAsync` when the fast-pipeline flag is on.

---

## Runtime services (hosted)

| Service | File | Purpose |
| ------- | ---- | ------- |
| `AudioCaseTakingBackgroundService` | `Niga-Domain/Services/AudioCaseTakingBackgroundService.cs` | Dequeues jobs; optional semantic-cache wait (0s on fast path) |
| `AudioCaseRetentionBackgroundService` | `Niga-Domain/Services/AudioCaseRetentionBackgroundService.cs` | Purge audio files if `AudioRetentionDays > 0` (currently 0 = never) |
| `AudioCaseZombieSessionSweeperBackgroundService` | `Niga-Domain/Services/AudioCaseZombieSessionSweeperBackgroundService.cs` | Fail stale Processing/Uploaded sessions |
| Embedding builders / refresh | `Services/AiEmbeddingInfrastructure/*` | Background embedding sync (not on request path) |

Queue: in-process `System.Threading.Channels` unbounded channel, **single reader**. Jobs are lost on process restart; startup requeues recent `Uploaded` sessions.

---

## Data stores

```text
Audio file  →  {cwd}/Data/AudioCaseTaking/{sessionId}{ext}
Transcript  →  AudioCaseSession.TranscriptRaw
Extraction  →  ConversationJson, SummaryJson, ExtractedSymptomsJson
Rubrics     →  SuggestedRubricsJson + AudioCaseRubricMatchLog
Events      →  AudioCaseSessionEventLog
AI calls    →  AudioCaseAiRequestLog
Consent     →  AudioCaseConsentLog
Doctor acts →  AudioCaseDoctorActionLog
Feedback    →  AudioCaseRubricFeedback
Concepts    →  AudioCaseClinicalConcept (+ session ClinicalConceptsJson)
```

No Redis. No Hangfire for audio jobs (`UseHangfireForIncrementalRefresh: false` applies to embeddings only).

---

## Authentication

- Backend: `[Authorize]` on `AudioCaseTakingController`. JWT Bearer.
- Frontend: `src/helpers/api_helper.js` attaches `Authorization: Bearer {sessionStorage.authUser.token}`.
- Intelligence health endpoint `GET /api/AudioCaseIntelligence/health` is `[AllowAnonymous]`.

---

## Diagrams

### Audio flow (implemented)

```text
Audio blob → multipart upload → disk
    → Whisper translations (English)
    → GPT JSON extraction (symptoms + summary + conversation)
    → Fast clinical retrieval
    → JSON result to UI
```

### Rubric flow (implemented, fast-f)

```text
Extracted symptoms
  → speaker/negation filter
  → multi-query concept expand
  → parallel DB/FTS/alias/embedding/catalog
  → quality / gender / hierarchy / hallucination gates
  → CanonicalScore
  → optional doctor-learning boost
  → MMR ≤ FastPipelineMaxFinalRubrics (12)
  → SubSectionId > 0 only
```

### Doctor feedback flow (implemented)

```text
Suggested rubrics
  → Approve / Reject in AudioCaseRubricApprovalBar
  → Add to repertorization (PatientBoard)
  → POST doctor-action (audit)
  → POST rubrics/feedback (learning + benchmark)
```

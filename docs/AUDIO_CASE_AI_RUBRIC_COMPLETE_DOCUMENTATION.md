# Audio Case & AI Rubric Engine — Complete Technical Documentation

**Audit date:** 2026-08-16  
**Method:** Source code + SQL scripts + existing dated docs. Git SHAs for every historical commit were not fully dumped; engine stamps and dated docs (`AI_RUBRIC_ENGINE_VS_FAST_PIPELINE.md`, 2026-08-13) reconstruct evolution.  
**Code changes in this task:** none.

**Companion files (same folder):**  
[Architecture](./AUDIO_CASE_ARCHITECTURE.md) · [API](./AUDIO_CASE_API_REFERENCE.md) · [Database](./AUDIO_CASE_DATABASE_REFERENCE.md) · [Prompts](./AUDIO_CASE_AI_PROMPTS.md) · [Rubric engine](./AUDIO_CASE_RUBRIC_ENGINE.md) · [Version history](./AUDIO_CASE_VERSION_HISTORY.md) · [Performance & accuracy](./AUDIO_CASE_PERFORMANCE_AND_ACCURACY.md)

Older `AI_RUBRIC_ENGINE_*.md` files remain. **If they disagree with this audit, source code wins.**

Frontend-side reports dated 31 Jul 2026 live in `NigaHomeopathy-UI/docs/AUDIO_CASE_TAKING_*.md`. They predate `fast-f` and must not be treated as current engine behavior.

---

## Master source-of-truth table

| Area | Current Implementation | Source | Status |
| ---- | ---------------------- | ------ | ------ |
| Frontend | React 18.3.1, Redux Toolkit, react-scripts 5, Bootstrap 5 / reactstrap, axios, Velzon template | `NigaHomeopathy-UI/package.json` | IMPLEMENTED |
| Audio | `useAudioRecorder` MediaRecorder + file upload + live waveform, 50 MB, webm/mp3/wav/ogg/m4a/aac | `useAudioRecorder.js`, `useAudioWaveform.js` | IMPLEMENTED |
| API | ASP.NET Core 8, JWT, `/api/AudioCaseTaking/*`, in-memory job queue | `AudioCaseTakingController.cs` | IMPLEMENTED |
| Whisper | OpenAI `whisper-1` via `audio/translations` (English) | `AudioCaseAiProcessor.TranslateAudioToEnglishAsync` | IMPLEMENTED |
| Transcript | Stored `AudioCaseSession.TranscriptRaw`; not overwritten by GPT | `ProcessSessionAsync` | IMPLEMENTED |
| AI | `gpt-4o` JSON extraction; optional V1 rubric-name GPT | `ExtractCaseDataAsync` | IMPLEMENTED |
| Rubric Search | Fast-f: parallel V1 FTS + keyword + alias + embedding + catalog | `FastClinicalRetrievalOrchestrator` | IMPLEMENTED |
| Embeddings | `text-embedding-3-small` 1536-d JSON vectors; in-memory cache | `EmbeddingSearchEngine`, `AiEmbeddingInfrastructure` | IMPLEMENTED (optional 8s channel) |
| Validation | QualityGate + gender/hierarchy/hallucination + enterprise 8-step | `FastClinicalEvidenceGate`, `ClinicalValidationEngine` | IMPLEMENTED |
| Ranking | CanonicalScore 0.30/0.25/0.20/0.15/0.10 + MMR λ=0.80 | `FastClinicalRanking` | IMPLEMENTED |
| Database | SQL Server `HomeoCentrum_Production`, `AudioCase*` + `SubSectionMaster` | SQL scripts + EF | IMPLEMENTED |
| Doctor Review | Approve/Reject UI + doctor-action + rubric feedback APIs | `AudioCaseRubricApprovalBar`, thunks | IMPLEMENTED |
| Performance | Discovery ~2–3 s; Whisper ~2 min; total ~2.5–9+ min historically | dated docs + timeouts in code | PARTIALLY MEASURED |

---

## 1. Executive Summary

HomeoCentrum Audio Case Taking records or uploads a consultation, transcribes it to English with OpenAI Whisper, extracts symptoms with GPT-4o, and discovers **database-backed** repertory rubrics (`SubSectionMaster`) using the **fast clinical retrieval** pipeline (`engineVersion = fast-f`).

The repository also contains V1–V7, V3 concept-graph GPT models, V6, and ECI v8. Those are **not** the production discovery path while `EnableFastClinicalRetrievalPipeline` is true.

Historical 15–20 minute runs match the **legacy V7/Enterprise** path (measured ~9.4 minutes with 314 s discovery). Fast-c was measured at **~2.5 minutes**, almost entirely Whisper. Target 2–3 minutes is **approachable after Whisper**, not after discovery.

Doctor acceptance for **fast-\*** engines is **not yet measured** (no feedback rows in the accuracy doc).

---

## 2. System Purpose

Let a doctor capture live or uploaded audio for a patient, obtain an English transcript and structured case summary, receive suggested repertory rubrics grounded in `SubSectionMaster`, approve/reject them, apply accepted ones to repertorization, and persist audit/learning signals.

It is **not** an automatic prescribing engine. Remedies appear after the doctor adds rubrics to the existing repertorization UI.

---

## 3. Current Architecture

See [AUDIO_CASE_ARCHITECTURE.md](./AUDIO_CASE_ARCHITECTURE.md).

```text
Frontend → JWT API → disk + SQL session → background worker
  → Whisper translations → GPT extraction → FastClinicalRetrieval
  → poll status/result → doctor approve/reject → feedback logs
```

---

## 4. Technology Stack

### Backend (`NigaHomeopathy-API`)

| Item | Actual |
| ---- | ------ |
| Runtime | .NET 8 (`net8.0`) |
| Web | `Niga-Web` (Swagger / Swashbuckle 6.4) |
| Domain | `Niga-Domain` (services, EF, hosted workers) |
| ORM | EF Core SQL Server 9.0.0-preview.3 |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.4) |
| JSON | Newtonsoft on MVC + `System.Text.Json` in AI processors |
| Tests | xUnit in `Niga-Domain.Tests` |
| Docker / CI | **Not found** in this repo |
| Queue | `System.Threading.Channels` in-process, single reader |

### Frontend (`NigaHomeopathy-UI`)

| Item | Actual |
| ---- | ------ |
| Framework | React 18.3.1 |
| Language | JavaScript (not TypeScript for Audio Case files) |
| Build | `react-scripts` 5.0.1 (Create React App), not Vite |
| UI | Bootstrap 5.3.3, reactstrap, react-bootstrap |
| State | Redux Toolkit 2.3 + react-redux |
| Forms | Formik/Yup exist in app; Audio Case uses local/Redux state |
| Audio | Browser `MediaRecorder` / `getUserMedia` (no extra audio npm lib) |
| HTTP | axios 1.7.7 |
| Auth | JWT in `sessionStorage.authUser` |
| Routing | react-router-dom 6 |

### Other workspace repos

- `NIGA_Latest_Code_API`: ASP.NET Core **2.2** `NIGA.Centrum` repertory-admin API. Classic `SubSection` FTS via `CONTAINSTABLE(... SearchNormalized ...)` and LIKE fallback. **No** Whisper/OpenAI/embeddings/Audio Case. Still maintained for master CRUD (last seen commit theme 2026-08-04). Shares `HomeoCentrum_Production` conceptually.
- `minimal`: template UI — **no** Audio Case components found.
- `NigaHomeopathy-UI/docs/AUDIO_CASE_TAKING_*.md`: frontend reports compiled **31 Jul 2026** (V1–V8 pack). **Stale relative to fast-f.**

---

## 5. Repository Structure (audio/rubric relevant)

```text
NigaHomeopathy-API/
  Niga-Web/Controllers/AudioCaseTakingController.cs
  Niga-Web/appsettings.json                 # flags + OpenAI (secrets present — redact)
  Niga-Domain/Repositories/AudioCaseTakingService.cs
  Niga-Domain/Services/AudioCaseAiProcessor.cs
  Niga-Domain/Services/AudioCaseTakingBackgroundService.cs
  Niga-Domain/Services/AudioCaseIntelligence/**   # engines V2–V8 + fast path
  Niga-Domain/Services/AiEmbeddingInfrastructure/**
  Niga-Domain/Master/AudioCase*.cs
  Database/Scripts/AudioCaseTaking_CreateTables.sql
  Database/Scripts/AudioCaseIntelligenceV2/**
  Niga-Domain.Tests/RubricIntelligence/**
  docs/                                     # this package + older AI_RUBRIC_ENGINE_* 

NigaHomeopathy-UI/
  src/Components/CaseTaking/AudioCase*.js
  src/hooks/useAudioRecorder.js
  src/slices/doctor/audioCaseTaking/
  src/helpers/audioCaseTakingHelper.js
  src/pages/Doctor/PatientBoard/PatientBoard.js   # hosts panel
```

---

## 6. Implementation Version History

See [AUDIO_CASE_VERSION_HISTORY.md](./AUDIO_CASE_VERSION_HISTORY.md).

**CURRENT:** `fast-f`. **ROLLBACK:** `EnableFastClinicalRetrievalPipeline=false` (V3/V7/Enterprise). **DISABLED:** V6, ECI v8.

---

## 7. Complete End-to-End Flow

Implemented stages only:

```text
Audio Input
    ↓ Frontend recording or file pick
    ↓ Client MIME/extension/size check (50 MB)
    ↓ POST upload (consent required)
    ↓ Store file under Data/AudioCaseTaking
    ↓ Queue ProcessAudio
    ↓ Whisper translations → TranscriptRaw
    ↓ GPT extraction → conversation/symptoms/summary JSON
    ↓ Dual-language helper (ignored by fast retrieval; extra Whisper only if non-English)
    ↓ FastClinicalRetrieval (parallel DB/semantic channels)
    ↓ Enterprise clinical validation
    ↓ Persist rubrics JSON + match logs
    ↓ UI poll → result
    ↓ Doctor approve/reject/add
    ↓ doctor-action + optional rubric feedback
```

**PLANNED / NOT CURRENTLY IMPLEMENTED**

- WebSocket progress
- Redis/Hangfire audio jobs
- Enforced `MaxAudioDurationMinutes` (config exists, **never read** outside Options)
- Auto-apply high-confidence rubrics (`AllowAutoApplyHighConfidence: false`)
- Automated fleet Precision@10 in production UI

---

## 8. Frontend Architecture

Host: `PatientBoard.js` when `caseTakingMode === 'audio'`. Entry: Dashboard `CaseTakingModeModal` (Manual vs Audio) → `buildPatientBoardAudioPath`.

Redux slice: `state.AudioCaseTaking` (`src/slices/doctor/audioCaseTaking/reducer.js`).

API base: `src/config.js` currently `API_URL_NIGAHOMEOPATHY: https://api1.homeocentrum.com/api` (localhost block is commented). `.env` `REACT_APP_API_URL` is **not** used for NIGA audio calls. **No `.env.example`.**

Admin (not doctor panel): `src/pages/Admin/RubricIntelligence/*` — metaphors, aliases, benchmark.

**UI gaps vs `fast-f` stamp (CONFIRMED):**

- `AudioCaseConceptTimeline` renders **only** if `engineVersion === 'v2'` — hidden on `fast-f` even when concepts are loaded.
- Explainability expand in `AudioCaseRubricSuggestions` is gated to `v2|v4.0|v5.2|v6.0|v7.0` — **not** `fast-f`.
- `correctedSubSectionId` is accepted by the feedback thunk but **never set by UI** (no in-panel rubric remap).
- Intensity in panel is default `#2`; no per-rubric intensity editor.
- No cancel of in-flight upload/poll; no upload byte-progress.
- Offline helper enqueues **metadata only** (no audio blob); `list`/`remove` unused.
- `checkForPreviousAudioCaseSession` / resume-latest exists in Redux but **Panel does not call it** (uses session history instead).
- `getMockAudioCaseAnalysisResult` is unused by the current thunk.

---

## 9. Audio Recording & Upload

### 9.1 `useAudioRecorder`

```text
Component/Hook: useAudioRecorder
File: NigaHomeopathy-UI/src/hooks/useAudioRecorder.js
Purpose: Live microphone capture
Inputs: none
Outputs: blob, durationMs, isRecording, isPaused, error
State: MediaRecorder + chunks every 1000 ms
API calls: none
Dependencies: MediaRecorder, getUserMedia
Validation: browser support
Error handling: NotAllowedError → permission message; else unable to access mic
```

MIME preference: `audio/webm;codecs=opus`, `audio/webm`, `audio/mp4`, `audio/ogg;codecs=opus`.  
Pause/resume: **implemented**.  
Duration limit: **not enforced** in hook.  
Waveform: `useAudioWaveform.js` — 48-bar `AnalyserNode` levels while recording (not paused).

### 9.2 `AudioCasePanel`

```text
Component: AudioCasePanel
File: src/Components/CaseTaking/AudioCasePanel.js
Purpose: Studio UI — record, upload, consent, language, analyze, results, history
API: uploadAndAnalyzeAudioCase, poll, reanalyze, download, history, feedback
```

Consent checkbox required before analyze. Language select: auto, en, hi, mr, gu, ta, te, kn, bn.

### 9.3 File upload

```text
Helper: isAcceptedAudioFile
File: audioCaseTakingHelper.js
MIME: mpeg/mp3/wav/x-wav/webm/ogg/aac/mp4/x-m4a
Extensions: mp3|wav|webm|ogg|m4a|aac
Max size: 50 * 1024 * 1024
```

Backend also allows `.mp4`. Frontend regex does not list `mp4` extension explicitly (MIME `audio/mp4` is accepted).

### 9.4 Frontend audio sequence

```text
User clicks Record
↓ getUserMedia({ audio: true })
↓ MediaRecorder.start(1000)
↓ optional pause/resume
↓ stop → Blob (webm/mp4/ogg)
↓ consent + Analyze
↓ FormData audioFile, patientId, consentGiven=true, audioSource, language
↓ POST /AudioCaseTaking/upload
↓ poll GET .../status every 2.5s
↓ GET .../result
```

Offline: failed upload may `enqueueOfflineAudioUpload` (`audioCaseOfflineQueueHelper.js`).

---

## 10. Frontend API Integration

See [AUDIO_CASE_API_REFERENCE.md](./AUDIO_CASE_API_REFERENCE.md).

Retry: poll loop only; upload errors queue offline helper; no exponential backoff library.  
Timeout: poll 8 min UX; backend 20 min. Mismatch is **intentional** (comment in thunk).

---

## 11. Backend Architecture

- Controllers in `Niga-Web` / `Niga-Domain.API.Controllers`
- Business in `Niga-Domain/Repositories` (service classes live here historically) and `Services/`
- DI: `ApplicationServiceExtensions.AddApplicationServices`
- Logging: `ILogger<T>`
- Exceptions: controller try/catch → helper Error; background FailSessionAsync
- Config: `appsettings.json` sections `AudioCaseTaking`, `RubricIntelligence`, `OpenAI`, `AzureOpenAI`, `AiEmbeddingInfrastructure`
- Runtime flag overrides: `RubricIntelligenceSettingsService` (in-memory, process lifetime)

---

## 12. Audio APIs

See API companion. Processing pipeline per upload:

```text
Upload endpoint → validation → disk → AudioCaseSession Uploaded → Enqueue
Background → ProcessSessionAsync
  → Whisper
  → RunExtractionAndRubricsAsync
  → MatchRubricsWithIntelligenceAsync (fast-f)
  → SuggestedRubricsJson Completed
Status/Result read session JSON
```

---

## 13. Whisper Transcription

```text
Feature: Whisper English translation
File: Niga-Domain/Services/AudioCaseAiProcessor.cs
Function: TranslateAudioToEnglishAsync
API: POST {OpenAI.BaseUrl}/audio/translations
Status: IMPLEMENTED and ACTIVE when OutputEnglishOnly=true (default)
```

| Item | Value |
| ---- | ----- |
| Provider | OpenAI (Azure section exists; `PreferAzureOverOpenAi: false`) |
| Model | `whisper-1` |
| Format | original upload bytes; no server transcode found |
| Preprocessing | none found |
| Max duration | config 45 min **not enforced** |
| Language | translations → English; `detectedLanguage` forced `"en"` on that path |
| Temperature | not set |
| Response | `verbose_json`; code reads `.text` |
| Cleanup | none beyond storing string |
| Retry | none |
| Timeout | HttpClient 10 minutes |
| Storage | file remains on disk; transcript in SQL |
| Cost | `EstimatedCostUsd` column exists on AI log; fill not confirmed in Whisper logger |

**Where transcript lives**

1. Generated: Whisper response `text`
2. Stored: `AudioCaseSession.TranscriptRaw`
3. Retrieved: `GetResultAsync`
4. Passed to GPT: user message `Transcript:\n\n{transcript}`
5. Displayed: Redux `audioCase.transcript` / `AudioCaseTranscriptEditor`

Mock: if no API key and `UseMockWhenNoApiKey` (appsettings **false** in current file; class default true).

---

## 14. Transcript Processing

| Step | Status |
| ---- | ------ |
| Cleaning / normalization | NOT IMPLEMENTED as a dedicated stage |
| Sentence splitting | NOT IMPLEMENTED |
| Speaker handling | GPT `conversation[].role` doctor\|patient — model-inferred, not diarization |
| Patient/doctor separation | Same |
| Medical terminology normalization | Prompt-level only |
| Duplicate removal | Prompt asks to ignore filler |
| Truncation | Whisper text stored whole; GPT must not echo it |
| Token limits / chunking | Single GPT call with full transcript; **no chunking** |
| Summarization | `summary` object from GPT |
| Clinical extraction | `symptoms[]` from GPT |

CONFIRMED: no separate NLP pipeline between Whisper and GPT.

---

## 15. AI / GPT Architecture

Every chat call uses HTTP `chat/completions` with Bearer `OpenAI:ApiKey`.

| Call | Model | Temp | Max tokens | Format |
| ---- | ----- | ---- | ---------- | ------ |
| ExtractCaseDataAsync | gpt-4o | 0.1 | 8192 | json_object |
| SuggestAiRubricsAsync | gpt-4o | 0.2 | unset | json_object |
| IntelligenceGptClient (legacy engines) | gpt-4o | 0.1 | unset | json_object |

Token usage logged on `AudioCaseAiRequestLog`. No cost aggregator UI confirmed for doctors.

Azure OpenAI is configured but not preferred.

---

## 16. AI Prompts

Full current prompts: [AUDIO_CASE_AI_PROMPTS.md](./AUDIO_CASE_AI_PROMPTS.md).

---

## 17. Clinical Information Extraction

**Fields that actually exist** on `AudioCaseSymptomModel` / `AudioCaseSummaryModel`:

- Symptom: `phrase`, `searchTerms`, `category` (particular\|general\|mental), `intensityHint` 1–4, `isSensationBearing`, `originalLanguageText`, `languageCode`
- Summary: `chiefComplaint`, `historyOfPresentIllness`, `mentals[]`, `generals[]`, `modalities[]`, `particulars[]`, `redFlags[]`
- Conversation: `role`, `text`, `timestamp`

**Not first-class schema fields** (may appear only inside free-text phrase/summary): dedicated location, sensation, time, duration, frequency, concomitants, causation, food craving/aversion, sleep, dreams, fears, sexual, menstrual, family/past history objects.

M5 legacy categories (Fear, Sleep, Dream, Sexual, …) are **not** extracted as typed objects on the fast path.

---

## 18. Rubric Discovery Engine

See [AUDIO_CASE_RUBRIC_ENGINE.md](./AUDIO_CASE_RUBRIC_ENGINE.md).

---

## 19. Database Rubric Search

`SearchSubSectionsByHotspotAsync` — CONTAINS FTS then Contains fallback; word-boundary filter. Table `SubSectionMaster.SubSectionName`. Details in database companion.

---

## 20. Embedding Architecture

Provider OpenAI `text-embedding-3-small`, 1536 dims, JSON float arrays.  
Input: concept query strings.  
Storage: `RubricEmbeddings` + `AiRubricEmbedding`.  
Search: in-memory cosine, top 50, min 0.74.  
Incremental refresh: V4 infrastructure background services.  
Fast path timeout 8s; proceeds with zero embedding hits if cache cold.

---

## 21. Semantic Search

Two different “semantic” meanings:

1. **V1 `ComputeTextSimilarity`** — lexical Jaccard-like (not vectors). Active inside V1 scoring (0.4 weight).
2. **Embedding cosine** — true vector search. Active as parallel fast-f channel.

---

## 22. AI Rubric Suggestions

V1 can call GPT to invent repertory-style names. Fast-f drops `SubSectionId <= 0`. Reconciler maps names to DB if they remain. Distinguish:

```text
AI GENERATED CANDIDATE  → IsAiSuggested / SubSectionId 0 / Source AiSuggested
DATABASE VERIFIED RUBRIC → SubSectionId > 0, IsDbBacked
```

UI `mapSuggestedRubricToRepertorization` returns null for `resultKind=aiclinicalconcept` or missing id.

---

## 23. Rubric Validation

See rubric engine companion. Enterprise 8 steps still run after fast-f because `EnableEnterpriseClinicalValidation: true`.

---

## 24. Rubric Ranking

Actual fast-f formula (hardcoded weights):

```text
0.30 Evidence + 0.25 ClinicalMatch + 0.20 Semantic + 0.15 ExactAlias + 0.10 Keyword
```

Then MMR. HybridWeights in appsettings are **legacy V2**.

---

## 25. Hallucination Prevention

Implemented: DB id required; evidence overlap; enterprise Hallucination step on leftover path; extraction prompt forbids inventing diagnoses.

Not implemented: blocking `SuggestAiRubricsAsync` from running on fast path (wasted call still possible).

---

## 26. Duplicate Prevention

- Candidate dictionary keyed by `SubSectionId` in V1
- `RubricResultMerger.Merge`
- MMR name Jaccard
- `DeduplicateAiConceptsWhenDbMatched` in QualityGate
- Match log replace-all for session

---

## 27. Doctor Review

```text
AI Result
 ↓ AudioCaseRubricSuggestions
 ↓ RequireManualApprovalForAllAiRubrics=true → Approve / Reject
 ↓ Approve → mapSuggestedRubricToRepertorization → PatientBoard repertorization
 ↓ Reject → local approval state + logAudioDoctorAction + submitAudioCaseRubricFeedback
 ↓ Transcript edit → reanalyze
```

No in-panel edit of rubric name or mapping: `correctedSubSectionId` is on the feedback API/thunk but **the UI never sends it**. Custom add/remove of repertory rubrics is the existing PatientBoard Repertory tab, not the audio panel.

Auto-apply exists in `AudioCasePanel` only when `requireManualApprovalForSuggestedRubrics` is false (currently true). History-opened sessions skip auto-apply. “Add all” is also hidden when manual approval is required.

---

## 28. Database Schema

See [AUDIO_CASE_DATABASE_REFERENCE.md](./AUDIO_CASE_DATABASE_REFERENCE.md).

---

## 29. Database Relationships

```text
SectionMaster → SubSectionMaster → RubricRemedyDetail → RemedyMaster
                              ↘ RubricEmbeddings / AiRubricEmbedding

AudioCaseSession → EventLog, AiRequestLog, Consent, MatchLog,
                   DoctorAction, Feedback, Concepts, IntelligenceLog
```

---

## 30. Data Persistence

Session JSON columns are the doctor-facing result store. Match logs are the ranked rubric audit. AI logs may store request/response JSON (PHI risk).

---

## 31. Configuration

**Do not copy secrets.** `Niga-Web/appsettings.json` currently contains live `OpenAI:ApiKey`, JWT Secret, SMTP password, WhatsApp token. Treat as **P0 security issue**. Use `[REDACTED]` in any copy.

| Variable / key | Purpose | Used in | Required | Default / current |
| -------------- | ------- | ------- | -------- | ----------------- |
| ConnectionStrings:DefaultConnection | SQL | EF | yes | localhost HomeoCentrum_Production |
| OpenAI:ApiKey | OpenAI auth | AudioCaseAiProcessor, EmbeddingClient | yes for real AI | [REDACTED] |
| OpenAI:BaseUrl | API host | HttpClient | no | https://api.openai.com/v1 |
| OpenAI:WhisperModel | Whisper | processor | no | whisper-1 |
| OpenAI:ChatModel | GPT | processor | no | gpt-4o |
| OpenAI:EmbeddingModel | embeddings | EmbeddingClient | no | text-embedding-3-small |
| AudioCaseTaking:StoragePath | audio files | service | no | Data/AudioCaseTaking |
| AudioCaseTaking:MaxFileSizeBytes | upload cap | UploadAsync | no | 52428800 |
| AudioCaseTaking:MaxAudioDurationMinutes | **unused** | Options only | — | 45 |
| AudioCaseTaking:OutputEnglishOnly | translations vs transcriptions | ProcessSessionAsync | no | true |
| AudioCaseTaking:EnableAiSuggestedRubrics | V1 GPT names | MatchRubricsV1Async | no | true |
| AudioCaseTaking:MaxAiSuggestedRubrics | cap | V1 | no | 25 (class default 10) |
| AudioCaseTaking:MaxProcessingMinutes | job cancel | CreateProcessingTimeoutSource | no | **20** (class default 10) |
| AudioCaseTaking:UseMockWhenNoApiKey | mock | processor | no | **false** in json |
| RubricIntelligence:EnableFastClinicalRetrievalPipeline | production gate | MatchRubricsWithIntelligenceAsync | no | **true** |
| RubricIntelligence:FastPipelineEngineVersion | stamp | orchestrator | no | fast-f |
| RubricIntelligence:FastPipelineMaxFinalRubrics | final K | fast path | no | 12 |
| RubricIntelligence:FastPipelineMmrLambda | MMR | ranking | no | 0.80 |
| RubricIntelligence:FastPipelineMinCanonicalScore | floor | ranking | no | 0.45 |
| RubricIntelligence:FastPipelineMinEvidenceScore | hallucination | evidence gate | no | 0.15 |
| RubricIntelligence:FastPipelineEmbeddingTimeoutSeconds | embed budget | orchestrator | no | 8 |
| RubricIntelligence:EmbeddingTopK | vector K | EmbeddingSearchEngine | no | 50 |
| RubricIntelligence:MinEmbeddingCosineForCandidate | cosine floor | search | no | 0.74 |
| RubricIntelligence:RequireManualApprovalForAllAiRubrics | UI gate | settings | no | true |
| RubricIntelligence:EnableV2 | must be true for fast path (`IsV2Active`) | settings | no | true |
| RubricIntelligence:RollbackToV1Only | skip V2/fast if used with EnableV2 | settings | no | false |
| JWT:Secret | tokens | auth | yes | [REDACTED] |

Azure keys empty; unused when PreferAzureOverOpenAi is false.

Frontend: no `.env.example` found; API URL hardcoded in `src/config.js`.

---

## 32. Security

| Topic | Finding |
| ----- | ------- |
| Authentication | JWT `[Authorize]` on audio APIs |
| Authorization | Session scoped to `DoctorUserId` from token |
| API key protection | Key in appsettings **in repo** — CONFIRMED risk |
| Upload validation | Extension allow-list + size; MIME from client not strictly matched to magic bytes |
| Path traversal | Stored name is `{guid}{extension}` under StoragePath |
| SQL injection | EF + parameterized FromSqlRaw `{0}` for FTS |
| Prompt injection | Transcript sent raw to GPT; no sanitizer found |
| Transcript logging | AI request/response JSON may include transcript |
| CORS | `AllowedHosts: *` |
| Rate limiting | **Not found** on audio endpoints |
| Frontend token logging | `api_helper.js` logs first 20 chars of JWT |

---

## 33. Error Handling

| Error | Detection | Backend | Frontend | Retry | User message | Logging |
| ----- | --------- | ------- | -------- | ----- | ------------ | ------- |
| Invalid/empty audio | Length 0 / missing |  Failure | Error banner | offline queue | file required | LogError |
| Unsupported format | extension set | Failure | same | no | Unsupported audio file type | |
| File too large | MaxFileSizeBytes | Failure | helper 50MB | no | exceeds maximum | |
| Whisper fail | HTTP/exception | FailSession PROCESSING_FAILED | poll failed | no | translation/transcription failed | AiRequestLog |
| OpenAI timeout | 10 min client / 20 min job | FailSession PROCESSING_TIMEOUT | poll timeout UX | Continue waiting | exceeded N-minute limit | |
| Rate limit | HTTP body | fail | message | no | response body | |
| Invalid JSON | deserialize | extraction fail session | failed | no | Case extraction failed | |
| Truncated JSON | no dedicated detector | likely deserialize fail | failed | no | | |
| DB fail | EF exception | FailSession unwrap inner | failed | no | SQL text truncated to 2000 | |
| Embedding fail | catch in SafeEmbeddingAsync | continue without | still completes | no | silent warn | |
| No candidates | empty list | Completed with 0 rubrics | empty hint | reanalyze | | |
| Frontend 8 min | poll attempts | still running | takingLonger message | manual continue | Analysis is taking longer… | |
| Mic denied | NotAllowedError | n/a | upload instead | | permission denied | |

Duration over 45 min: **not detected**.

---

## 34. Logging & Monitoring

- CorrelationId (12 hex) on session
- `AudioCaseSessionEventLog` step trail including `LatencyGap1_*` / `LatencyGap3_*`
- `AudioCaseAiRequestLog` per OpenAI call
- `IRubricPipelineTelemetry` stages → intelligence log / `PipelineBaselineSummary` (`EngineVersion` constant `base-a` for some telemetry rows — **can disagree** with session `fast-f`)
- `EnableAiMonitoringDashboard: true` — admin benchmark endpoints
- No Datadog/App Insights package found in csproj

**Debug one case:** take `sessionId` → event log ordered → AI log latencies → match log → `SuggestedRubricsJson`.

---

## 35. Performance Analysis

See [AUDIO_CASE_PERFORMANCE_AND_ACCURACY.md](./AUDIO_CASE_PERFORMANCE_AND_ACCURACY.md).

---

## 36. Current Bottlenecks

1. Whisper wall clock (~100–140 s measured)
2. Single-reader in-memory queue
3. Optional extra Whisper (dual-language, non-English)
4. Optional extra GPT inside V1 suggestions
5. Cold embedding cache (degraded recall, not usually latency)
6. Legacy path if flag flipped (V7 ~5 min discovery)

---

## 37. Accuracy Analysis

No automated production Precision/Recall. Tiny historical samples: v2 accept 50%; v7 n=4 100%; fast-* **none**. False positive/negative rates **UNKNOWN**.

---

## 38. Current Limitations

- One GPT extraction; missed symptoms are not recovered on fast path
- No audio duration enforcement
- Queue not durable
- Health API reports v1/v2 not fast-f
- Frontend 8 min vs backend 20 min
- Prompts and keys in source/config
- `englishTranscript` fixed; large conversation JSON can still truncate
- Manual approval always on

---

## 39. Legacy Implementations

| Old | Location | Why replaced | Replacement | Referenced? | Delete? |
| --- | -------- | ------------ | ----------- | ----------- | ------- |
| V7 discovery | RepertoryIntelligence | Too slow (314 s) | fast-f | Rollback | No |
| V3 M0–M5 GPT | V3/Engines | Latency / duplicate GPT | extraction + FTS | Rollback | No |
| V6 | V6/ | Flag off | — | Tests | No |
| ECI v8 | ECI/V8 | Flag off | — | Tests | No |
| NIGA.Centrum API | sibling repo | Different product surface | NigaHomeopathy-API | No audio | N/A |

---

## 40. Duplicate Implementations

- Hotspot search used by V1, Keyword, Hierarchical, V6 SQL engines
- Two embedding stacks (legacy `RubricEmbeddings` vs `AiRubricEmbedding`)
- Two “semantic” scores (Jaccard vs cosine)
- Dual-language built then unused on fast path
- Telemetry engine `base-a` vs session `fast-f`
- Docs `AI_RUBRIC_ENGINE_*` vs this audit
- Controllers: Taking vs Intelligence vs IntelligenceV3 vs Admin vs Embedding

---

## 41. Testing

**Existing:** xUnit tests under `Niga-Domain.Tests/RubricIntelligence/` (FastClinicalRanking, FastClinicalRetrievalStageC, accuracy packs, validation, embeddings, V7, ECI, etc.) and `AiEmbeddingInfrastructure/`.

**Not found:** frontend Audio Case tests; API integration tests hitting Whisper; Playwright E2E.

Run: `dotnet test Niga-Domain.Tests/Niga-Domain.Tests.csproj`

**Recommended matrix**

| Area | Unit | Integration | Notes |
| ---- | ---- | ----------- | ----- |
| Audio upload | extension/size | multipart | duration currently untested |
| Transcript | overwrite EnglishTranscript | Whisper mock | |
| Extraction JSON | schema deserialize | golden transcripts | |
| Discovery | ranking/MMR/gates | FTS on SQL | |
| Validation | enterprise steps | | |
| Embedding | cosine math | cache empty | |
| Ranking | weight formula | | |
| Doctor review | — | feedback types | |

---

## 42. Debugging Guide

### Case A — Transcription fails

Start: `AudioCaseAiRequestLog` ServiceType Translation/Transcription. Files: `AudioCaseAiProcessor.cs`, `ProcessSessionAsync`. Expect: Failed + error body. Causes: key, timeout, bad file, OpenAI outage. Disk: `AudioFilePath`.

### Case B — Transcript OK, no rubrics

Start: `ExtractedSymptomsJson` empty? Then FastClinicalRetrieval log `final=0`. Causes: extraction empty; gates too strict; FTS down falling back poorly; all scores &lt; 0.45.

### Case C — Wrong rubrics

Start: MatchLog `MatchedFrom` vs `SubSectionName`. Inspect QualityGate hitchhikers; extraction searchTerms (fit vs convulsion). Reanalyze after transcript edit.

### Case D — AI rubric not in DB

Should not appear as addable on fast-f (`subSectionId>0` required). If it does, check `EnableFastClinicalRetrievalPipeline` and reconciler. `SuggestAiRubrics` log shows invented names.

### Case E — Only 2–3 rubrics

Expected if MMR pool small. Check funnel counts in intelligence log (`v1=; kw=; emb=`). Do not assume a bug if clinically sparse.

### Case F — 15+ minutes

Likely **legacy path** or **queue wait** or **Whisper + dual-language**. Check `IntelligenceEngineVersion` on session. If `v7.0`, fast flag is off. Event `SemanticCacheWait` 120s = old behavior.

### Case G — Invalid/truncated JSON

Extraction `max_tokens` 8192. Confirm `LlmExtraction` AI log response. `englishTranscript` echo is fixed. Retry reanalyze.

---

## 43. API Reference

See [AUDIO_CASE_API_REFERENCE.md](./AUDIO_CASE_API_REFERENCE.md).

---

## 44. Database Reference

See [AUDIO_CASE_DATABASE_REFERENCE.md](./AUDIO_CASE_DATABASE_REFERENCE.md).

---

## 45. End-to-End Example

Fictional patient. **Actual system behavior** marked.

Patient says: *"I get severe headache on the right side when exposed to sunlight."*

```text
Audio (webm blob)
    ↓ ACTUAL: MediaRecorder or file
Whisper translations
    ↓ ACTUAL: English transcript stored on session
GPT extraction
    ↓ ACTUAL likely fields: phrase ~ "severe right-sided headache from sunlight"
      category particular; searchTerms e.g. headache, right, sun; modalities in summary
Speaker filter
    ↓ ACTUAL: keep if patient-affirmed
Parallel search
    ↓ ACTUAL: FTS/Contains on SubSectionName for terms; alias; embedding if cache warm; catalog tokens
Validation
    ↓ ACTUAL: drop SubSectionId 0; evidence overlap; gender if known
Ranking
    ↓ ACTUAL: CanonicalScore + MMR ≤ 12
Final rubrics
    ↓ ACTUAL: names from SubSectionMaster only (e.g. HEAD pain / sun related rows IF they exist in DB)
Doctor
    ↓ ACTUAL: Approve adds to repertorization; Reject logs feedback
```

Exact rubric strings **depend on the live SubSectionMaster corpus** — not invented here.

---

## 46. Architecture Diagrams

See architecture companion. Additional:

```text
Doctor browser
    → api1.homeocentrum.com/api  (current UI config)
    → Niga-Web
    → OpenAI api.openai.com
    → SQL Server HomeoCentrum_Production
    → local disk Data/AudioCaseTaking
```

---

## 47. Current Implementation Status

| Feature | Status | Evidence | File | Notes |
| ------- | ------ | -------- | ---- | ----- |
| Audio recording | IMPLEMENTED | MediaRecorder | useAudioRecorder.js | pause/resume yes |
| Audio upload | IMPLEMENTED | FormData | thunk.js | 50 MB |
| Whisper | IMPLEMENTED | translations | AudioCaseAiProcessor.cs | |
| Transcript | IMPLEMENTED | TranscriptRaw | AudioCaseSession | |
| AI extraction | IMPLEMENTED | gpt-4o JSON | ExtractCaseDataAsync | |
| DB rubric search | IMPLEMENTED | FTS/Contains | SubSectionRepository | |
| Embedding search | IMPLEMENTED | optional 8s | EmbeddingSearchEngine | skip if cold |
| AI suggestion | PARTIALLY IMPLEMENTED | V1 GPT names often dropped | SuggestAiRubricsAsync | |
| Rubric validation | IMPLEMENTED | gates + enterprise | FastClinicalEvidenceGate | |
| Ranking | IMPLEMENTED | Canonical+MMR | FastClinicalRanking.cs | |
| Doctor review | IMPLEMENTED | Approve/Reject | AudioCaseRubricApprovalBar | no in-panel remap/intensity |
| Concept timeline | PARTIALLY IMPLEMENTED | hidden unless `engineVersion==='v2'` | AudioCaseConceptTimeline.js | invisible on fast-f |
| Rubric explainability UI | PARTIALLY IMPLEMENTED | gated to v2/v4/v5.2/v6/v7 | AudioCaseRubricSuggestions.js | not shown for fast-f |
| Live waveform | IMPLEMENTED | AnalyserNode 48 bars | useAudioWaveform.js | |
| Feedback | IMPLEMENTED | feedback API | DoctorFeedbackLearningEngine | |
| Performance tracking | PARTIALLY IMPLEMENTED | telemetry + SQL 730 | RubricPipelineTelemetry | N small |
| Duration limit | NOT IMPLEMENTED | unused option | AudioCaseTakingOptions | |
| WebSocket | NOT IMPLEMENTED | polling only | thunk.js | |
| Redis queue | NOT IMPLEMENTED | Channel | AudioCaseTakingQueue | |
| V6/ECI | NOT IMPLEMENTED as active | flags false | RubricIntelligenceOptions | code exists |
| Docker | NOT FOUND | — | — | |

---

## 48. Recommended Performance Improvements

See [AUDIO_CASE_PERFORMANCE_AND_ACCURACY.md](./AUDIO_CASE_PERFORMANCE_AND_ACCURACY.md). Do not mix with current behavior.

---

## 49. Recommended Accuracy Improvements

Same companion. Do not claim 90–95% targets are met.

---

## 50. Future Architecture

Not implemented. If designed later, keep: DB-backed finals, one extraction GPT, parallel retrieval, durable queue, Whisper as async stage with UI honesty. **Do not** re-enable V7 on the default path without a flag.

---

## 51. Developer Quick Start

Actual commands from the repos:

```text
1. Frontend:  cd NigaHomeopathy-UI && npm start
   (package.json: react-scripts start, NODE_OPTIONS 4096)
2. Backend:   cd NigaHomeopathy-API/Niga-Web && dotnet run --launch-profile http
   Swagger: http://localhost:5038/swagger
3. Environment: Niga-Web/appsettings.json (do not commit secrets)
4. Database: SQL Server; ConnectionStrings:DefaultConnection → HomeoCentrum_Production
5. OpenAI: OpenAI:ApiKey, WhisperModel, ChatModel, EmbeddingModel
6. Run SQL: Database/Scripts/AudioCaseTaking_CreateTables.sql
            then AudioCaseIntelligenceV2 MASTER guide
            then AIV4 embedding scripts as needed
            FTS: 725_FullText_SubSectionMaster_SubSectionName.sql
7. Verify rubrics: SELECT COUNT(*) FROM SubSectionMaster WHERE DeleteStatus=0
8. Verify embeddings: SELECT COUNT(*) FROM RubricEmbeddings
                      and/or AiRubricEmbedding
9. Point UI src/config.js at http://localhost:5038/api (commented block)
10. Patient Board → Audio case → consent → record/upload
11. Check TranscriptRaw / UI transcript
12. Check ExtractedSymptomsJson
13. Check SuggestedRubricsJson / match log
14. Approve/Reject → AudioCaseRubricFeedback
```

Tests: `dotnet test` on `Niga-Domain.Tests`. Frontend `npm test` is CRA default; no Audio Case specs found.

---

## 52. Complete File Reference

| Path | Role |
| ---- | ---- |
| `Niga-Web/Controllers/AudioCaseTakingController.cs` | HTTP API |
| `Niga-Web/Controllers/AudioCaseIntelligenceController.cs` | health/config/benchmark |
| `Niga-Web/appsettings.json` | production flags |
| `Niga-Domain/Repositories/AudioCaseTakingService.cs` | session + routing to engines |
| `Niga-Domain/Services/AudioCaseAiProcessor.cs` | Whisper + extraction + AI names |
| `Niga-Domain/Services/AudioCaseTakingBackgroundService.cs` | worker |
| `Niga-Domain/Services/AudioCaseTakingQueue.cs` | Channel queue |
| `Niga-Domain/Services/AudioCaseIntelligence/Orchestration/FastClinicalRetrievalOrchestrator.cs` | production discovery |
| `Niga-Domain/Services/AudioCaseIntelligence/Merging/FastClinicalRanking.cs` | scores + MMR |
| `Niga-Domain/Services/AudioCaseIntelligence/Merging/FastClinicalEvidenceGate.cs` | gates |
| `Niga-Domain/Services/AudioCaseIntelligence/Merging/RubricCandidateQualityGate.cs` | Bugs A–D |
| `Niga-Domain/Services/AudioCaseIntelligence/Merging/AiSuggestedRubricReconciler.cs` | AI→DB map |
| `Niga-Domain/Services/AudioCaseIntelligence/Engines/EmbeddingSearchEngine.cs` | vectors |
| `Niga-Domain/Services/AudioCaseIntelligence/Engines/ConceptKeywordDiscoveryEngine.cs` | per-concept FTS |
| `Niga-Domain/Repositories/SubSectionRepository.cs` | hotspot search |
| `Niga-Domain/Configuration/RubricIntelligenceOptions.cs` | all flags |
| `NigaHomeopathy-UI/src/Components/CaseTaking/AudioCasePanel.js` | UI |
| `NigaHomeopathy-UI/src/hooks/useAudioRecorder.js` | mic |
| `NigaHomeopathy-UI/src/slices/doctor/audioCaseTaking/thunk.js` | poll/upload |

---

## 53. Glossary

| Term | Meaning in this codebase |
| ---- | ------------------------ |
| Rubric | `SubSectionMaster` row (repertory heading) |
| SubSectionId | Rubric primary key |
| fast-f | Current retrieval engine stamp |
| V1 | Hotspot/FTS + optional GPT names |
| Concept | ClinicalConceptModel derived from extracted symptoms on fast path |
| AI suggested | Name without verified SubSectionId |
| CanonicalScore | 0–1 fast-path rank score |
| CorrelationId | 12-char job/session trace id |
| Repertorization | Existing doctor UI that scores remedies from selected rubrics |

---

## 54. Final Technical Assessment

### What Is Working

- Upload/record, consent, background processing, English Whisper, GPT extraction without echoing transcript
- Fast-f DB-backed rubric discovery in ~2–3 s
- Polling UI with stage labels, transcript editor, reanalyze, history, download
- Manual approval, feedback learning hooks, embedding infrastructure, extensive unit tests for ranking/gates

### What Is Partially Working

- Embeddings (timeout/skip)
- AI name suggestions (generated then dropped)
- Dual-language (built, unused on fast path)
- Performance/accuracy telemetry (tables exist, N too small)
- Health endpoint (wrong engine label)
- Concept timeline / explainability UI (gated to old engineVersion strings; hidden on `fast-f`)
- Offline upload queue (metadata only; no blob; never flushed)

### What Is Not Working / Not implemented

- Duration cap
- Durable queue
- Automated fleet accuracy
- Auto-apply
- Docker/CI in repo

### What Is Too Slow

- Whisper (dominant)
- Legacy V7 path if re-enabled
- Single worker queue under load

### What Is Reducing Accuracy

- Single-shot extraction misses
- MMR/min-score dropping valid candidates (false negatives)
- Cold embedding cache
- Possible V1 GPT names polluting V1 merge before drop (false positives if reconciler later maps badly on non-fast path)

### What Creates False Positives

- Broad Contains/FTS hits; mitigated by word-boundary and domain score 0.35
- Hitchhiker rubric tails; QualityGate
- Legacy V2 prompt mapping fit→convulsion (not on fast-f extraction)

### What Creates False Negatives

- Canonical floor 0.45, evidence 0.15, MMR cliff, max 12, embedding timeout

### What Creates AI Hallucinations

- `SuggestAiRubricsAsync` (contained on fast-f by SubSectionId filter)
- GPT extraction inventing symptoms (prompt forbids; not programmatically verified)

### What Creates Duplicate Work

- V1 + Keyword same hotspot API
- Dual-language unused
- V1 GPT suggestions discarded
- Multiple engine codebases still registered in DI

### What Is Legacy

- V3/V6/V7/ECI discovery; V2 orchestrator; `NIGA_Latest_Code_API` for this feature

### What Should Be Refactored

- Split `AudioCaseTakingService` (2000+ lines)
- Align health EngineVersion with fast-f
- Remove or gate V1 AI suggestions on fast path
- Secret storage

### What Should Be Optimized First

P0 Whisper UX/time; P0 skip wasted V1 GPT; P1 skip unused dual-language; P1 worker concurrency.

### What Should NOT Be Changed

- DB-backed-only finals
- Whisper as source of transcript
- Manual doctor approval default
- MMR “do not fabricate to fill 12”

### Recommended Priority

| ID | Item | Priority |
| -- | ---- | -------- |
| Remove secrets from appsettings / stop logging JWTs | P0 |
| Skip SuggestAiRubrics on fast path | P0 |
| Collect fast-f Accept/Reject (SQL 730) | P0 |
| Whisper latency / client compression | P0 |
| Gate dual-language on fast path | P1 |
| Multi-worker queue | P1 |
| Enforce duration + magic-byte MIME | P1 |
| Health API engine stamp | P2 |
| Durable jobs | P2 |
| Delete unused engines | P3 — do not delete until rollback unused |

---

## Current System Reality

1. **How does audio enter?** Live MediaRecorder or file → multipart POST `/api/AudioCaseTaking/upload`.
2. **Transcript?** OpenAI Whisper `audio/translations` (`whisper-1`) when `OutputEnglishOnly`.
3. **Stored?** `AudioCaseSession.TranscriptRaw` (+ file on disk).
4. **Clinical concepts?** One GPT-4o JSON extraction (`symptoms`, `summary`, `conversation`).
5. **DB search?** `SubSectionMaster` FTS CONTAINS / Contains hotspot, plus keyword/alias/catalog.
6. **Embedding search?** Yes, parallel, optional, cosine ≥ 0.74, 8 s timeout.
7. **AI suggestions?** V1 may generate names; fast-f does not keep unverified ids.
8. **Verified?** SubSectionId &gt; 0, reconciler exact/fuzzy if AI rows remain.
9. **Hallucinations?** Dropped if not in DB; evidence overlap gate.
10. **Ranked?** CanonicalScore weighted sum + MMR.
11. **How many?** Up to 12; fewer allowed; never padded.
12. **Doctor validation?** Approve/Reject UI; APIs doctor-action + rubrics/feedback; auto-apply off.
13. **Results stored?** Session JSON + match logs + feedback tables.
14. **Processing time?** Measured fast-c ~2.5 min (Whisper ~139 s); legacy ~9.4 min; 15–20 min is legacy/queue/timeout territory; P50 unproven.
15. **Biggest performance bottlenecks?** Whisper; single worker; leftover GPT/Whisper extras.
16. **Biggest accuracy bottlenecks?** Unmeasured acceptance; extraction misses; score floors.
17. **Legacy?** V3–V7/ECI discovery code; sibling Centrum API.
18. **Duplicated?** Multiple engines, two embedding tables, two semantic scores.
19. **Incomplete?** Duration limit, durable queue, fleet metrics, secret hygiene.
20. **Improve first?** Secrets, skip wasted GPT, measure fast-f acceptance, Whisper time.

---

## Confidence labels

CONFIRMED: fast-f is the discovery path; Whisper translations; GPT extraction; FTS search; CanonicalScore formula; 12 max; no duration enforcement; in-memory queue; englishTranscript overwrite.

LIKELY: production UI talks to `https://api1.homeocentrum.com/api` per `config.js` (may differ per deploy).

UNCERTAIN: live P50 latency for fast-f; production embedding cache hit rate; whether appsettings in git matches the hosted API.

NOT IMPLEMENTED: automated doctor acceptance-rate job that fills 95% claims; Redis; WebSocket; MaxAudioDurationMinutes enforcement.

LEGACY: V3/V7/ECI/V6 discovery when fast flag is on.

---

DOCUMENTATION AUDIT COMPLETE

Repository scanned: NigaHomeopathy-API, NigaHomeopathy-UI, NIGA_Latest_Code_API, minimal  
Frontend: NigaHomeopathy-UI (React 18 / CRA)  
Backend: NigaHomeopathy-API (ASP.NET Core 8)  
Database: SQL Server scripts + EF models (HomeoCentrum_Production)  
AI: OpenAI gpt-4o + whisper-1 + text-embedding-3-small  
Audio: MediaRecorder + multipart upload + Whisper translations  
Rubric Engine: FastClinicalRetrievalOrchestrator fast-f  
Embedding: optional cosine channel + V4 infra  
Git history: reconstructed from engine stamps and dated docs (2026-08-13); full SHA timeline not dumped  
Documentation files created: 8 under NigaHomeopathy-API/docs/AUDIO_CASE_*  
Major unknowns: fast-f fleet accuracy; hosted config drift; exact git SHAs per milestone  
Major risks: secrets in appsettings; PHI in AI logs; 8 vs 20 minute timeouts; wasted V1 GPT; Whisper-bound SLA  
Top 5 recommended improvements: (1) secret hygiene (2) skip V1 AI suggestions on fast path (3) measure doctor accept on fast-f (4) Whisper time/UX (5) durable multi-worker queue  

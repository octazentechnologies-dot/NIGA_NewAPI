# Audio Case API Reference

Base path: `/api/AudioCaseTaking`  
Auth: JWT Bearer (`[Authorize]`) unless noted.  
Frontend HTTP: `src/helpers/realbackend_helper.js` via `nigahomeoMultipart` / `nigahomeoAPI`.  
URL constants: `src/helpers/url_helper.js`.  
Thunks: `src/slices/doctor/audioCaseTaking/thunk.js`.

Response envelope (typical): `{ success, message, resultObject }` via `ThreeDBodyPartApiResponseHelper`. Frontend unwraps `resultObject` then `data`.

---

## Endpoints used by Audio Case UI

| API | Method | Route | Purpose | Frontend |
| --- | ------ | ----- | ------- | -------- |
| Upload | POST | `/api/AudioCaseTaking/upload` | Multipart audio + consent | `uploadAudioCaseTaking` |
| Status | GET | `/api/AudioCaseTaking/{sessionId}/status` | Poll progress | `getAudioCaseTakingStatus` |
| Result | GET | `/api/AudioCaseTaking/{sessionId}/result` | Transcript, summary, rubrics | `getAudioCaseTakingResult` |
| Re-analyze | POST | `/api/AudioCaseTaking/{sessionId}/reanalyze` | Re-run from edited transcript | `reAnalyzeAudioCaseTaking` |
| Doctor action | POST | `/api/AudioCaseTaking/{sessionId}/doctor-action` | Audit accept/reject/apply | `logAudioCaseDoctorAction` |
| Latest | GET | `/api/AudioCaseTaking/latest?patientId=&caseId=` | Resume last session | `getLatestAudioCaseSession` |
| Sessions | GET | `/api/AudioCaseTaking/sessions?patientId=&pageNumber=&pageSize=` | History | `getAudioCaseTakingSessions` |
| Concepts | GET | `/api/AudioCaseTaking/{sessionId}/concepts` | Clinical concepts | `getAudioCaseConcepts` |
| Rubric feedback | POST | `/api/AudioCaseTaking/{sessionId}/rubrics/feedback` | Learning + benchmark | `submitAudioCaseRubricFeedback` |
| Download | GET | `/api/AudioCaseTaking/{sessionId}/download` | Audio bytes | `downloadAudioCaseRecording` |

Handler: `Niga-Web/Controllers/AudioCaseTakingController.cs`.

---

### POST `/api/AudioCaseTaking/upload`

**Auth:** JWT. Doctor user id from token (`User.GetUserId()`), not trusted solely from form.

**Request:** `multipart/form-data` (`AudioCaseUploadRequestModel`)

| Field | Required | Notes |
| ----- | -------- | ----- |
| audioFile | yes | Size ≤ `MaxFileSizeBytes` (50 MiB) |
| patientId | yes | |
| caseId | no | |
| patientAppId | no | |
| audioSource | no | default `LiveRecording`; UI sends `LiveRecording` or `FileUpload` |
| originalFileName | no | |
| language | no | Whisper language override |
| consentGiven | yes | must be true |

Allowed extensions (backend): `.mp3 .wav .webm .ogg .m4a .aac .mp4`.

**Response:** `{ sessionId, status: "Uploaded" }`

**Errors:** invalid user, missing file, missing patient, no consent, file too large, unsupported type.

**DB:** insert `AudioCaseSession`, `AudioCaseConsentLog`, event logs; enqueue `ProcessAudio`.

**AI:** none at request time (background).

**Frontend timeout:** axios default (no special upload timeout found). Request size limit disabled on action (`[DisableRequestSizeLimit]`).

---

### GET `/api/AudioCaseTaking/{sessionId}/status`

**Response (`AudioCaseStatusModel`):** `sessionId`, `status`, `progressStep`, `stageLabel`, `percent`, `errorMessage`, `processingStartedAt`, `elapsedSeconds`, `engineVersion`.

Statuses include: `Uploaded`, `Processing`, `Transcribing`, `Extracting`, `MatchingRubrics`, `Completed`, `Failed`.

**Frontend poll:** every 2500 ms, max 192 attempts (~8 min), then stops auto-poll but does **not** mark failed (backend may still run up to 20 min).

---

### GET `/api/AudioCaseTaking/{sessionId}/result`

**Response (`AudioCaseResultModel`):**

- `transcript`
- `messages` (conversation)
- `summary` (chiefComplaint, HPI, mentals, generals, modalities, particulars, redFlags)
- `suggestedRubrics` (see rubric fields below)
- `rubricIntelligence` (`engineVersion`, `requireManualApprovalForSuggestedRubrics`, …)
- `processingMetrics` (optional telemetry)

---

### POST `/api/AudioCaseTaking/{sessionId}/reanalyze`

**JSON:** `{ "transcript": "..." }`

Skips Whisper. Re-queues `JobType: ReAnalyze`. Blocked if session already in-flight.

---

### POST `/api/AudioCaseTaking/{sessionId}/doctor-action`

**JSON (`AudioCaseDoctorActionRequestModel`):** `actionType`, `targetType`, `targetId`, `beforeJson`, `afterJson`, `notes`.

Writes `AudioCaseDoctorActionLog`. Does not change repertorization by itself.

---

### POST `/api/AudioCaseTaking/{sessionId}/rubrics/feedback`

**JSON:** `feedbackType` (`Approved`/`Accepted`, `Rejected`, `Edited`/`Corrected`), `rubricName` required, `subSectionId`, `correctedSubSectionId` for edits, `reason`, `rejectReasonStage`.

Handler: `DoctorFeedbackLearningEngine.ProcessFeedbackAsync`.

---

### GET download

Returns `File(bytes, contentType, fileName)` or `400 { success:false, message }`.

---

## Suggested rubric payload (actual DTO fields)

From `AudioCaseSuggestedRubricModel` — do not invent extra fields:

`subSectionId`, `subSectionName`, `sectionId`, `matchScore`, `suggestedIntensityNo`, `matchedFrom`, `remedyCountForSort`, `isAiSuggested`, `matchSource`, `confidenceScore`, `whySuggested`, `engineVersion`, `requiresManualApproval`, `homeopathicWeight`, `matchLayer`, `rubricTier`, `requiresDoctorReview`, `sourceConceptId`, `explainability`, `evidenceChain`, `qualityScore`, `validationStatus`, `validationFlags`, `resultKind`, `repertoryPath`, `canonicalScore`, `evidenceScore`, `isDbBacked`, …

Fast path filters to `subSectionId > 0` before response.

---

## Related intelligence APIs (admin / ops)

Controller: `AudioCaseIntelligenceController` route `/api/AudioCaseIntelligence`.

| Method | Route | Auth | Purpose |
| ------ | ----- | ---- | ------- |
| GET | `/health` | Anonymous | V2/rollback/repertory status (engineVersion reported as v2/v1 — **does not report fast-f**) |
| GET | `/config` | (see controller) | Runtime flags |
| PUT | `/config` | | Runtime overrides |
| GET | `/benchmark/summary` | | Benchmark |
| GET | `/benchmark/trends` | | Trends |
| GET | `/feedback/queue` | | Feedback queue |
| GET | `/rollout/status` | | Rollout |
| GET | `/repertory/status` | | Mapping |

Admin: `AudioCaseIntelligenceAdminController` — metaphors, aliases.  
V3: `AudioCaseIntelligenceV3Controller`.  
Embeddings: `AiEmbeddingInfrastructureController`.

Frontend admin URLs in `url_helper.js` (`RUBRIC_INTELLIGENCE_*`).

**Documented discrepancy:** `/health` `EngineVersion` is `"v2"` or `"v1"`, not `fast-f`. Session result `rubricIntelligence.engineVersion` is the real processing stamp.

---

## Headers

| Header | Set by |
| ------ | ------ |
| `Authorization: Bearer …` | `api_helper.js` interceptor |
| `Content-Type: multipart/form-data` | `nigahomeoMultipart` on upload |
| `Content-Type: application/json` | other calls |

No WebSocket. Status is HTTP polling only.

---

## Errors

Backend returns helper Failure/Error with message string. Frontend `assertApiSuccess` throws if `success === false`. Failed sessions: status `failed` + `errorMessage` / `errorCode` (`PROCESSING_TIMEOUT`, `PROCESSING_FAILED`, `PROCESSING_STALE`, `UPLOAD_STALE`, `UPLOAD_FILE_MISSING`, `BACKGROUND_JOB_FAILED`).

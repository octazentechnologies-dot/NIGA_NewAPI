# CURSOR CODEBASE AUDIT — 2026-08-17

**Scope:** fact-finding only. No fixes. No recommendations.  
**Rule:** every claim is `file:line` or **NOT FOUND IN CODE** / **NOT DETERMINED FROM THIS MACHINE**.  
**Repos inspected:** `NigaHomeopathy-API` (branch `GouravDev_29-6-26`, HEAD `7dc1148`), `NigaHomeopathy-UI` (branch `AudioCasetaking_29-6-26`, HEAD `65e36de`), plus `NIGA_Latest_Code_API` and `minimal` for the named document / `fast-f` search.  
**Live host probed:** `https://api1.homeocentrum.com/api` (2026-08-17 ~02:05–02:07 UTC).  
**Secrets:** connection strings, JWT, WhatsApp tokens, OpenAI keys from `appsettings*.json` are **not** copied into this report.

---

## 1. Deployment identification (branch, fast-f findings, superseding doc)

### 1.1 `AUDIO_CASE_AI_RUBRIC_COMPLETE_SINGLE_DOCUMENT.md`

**FOUND** (working tree only; not on any git branch searched).

| Search | Result |
| ------ | ------ |
| Recursive `find` under `/Users/OctazenWork/NIGA Project` | 1 hit: `NigaHomeopathy-UI/AUDIO_CASE_AI_RUBRIC_COMPLETE_SINGLE_DOCUMENT.md` (2517 lines) |
| `NigaHomeopathy-API` `git log --all -- '*AUDIO_CASE_AI_RUBRIC_COMPLETE_SINGLE_DOCUMENT*'` | empty |
| `NigaHomeopathy-UI` `git log --all` for that filename | empty |
| `git ls-tree` on UI `main`, `origin/main`, `origin/Gourav_CodeMerge_9-6-26` | not present |
| `NIGA_Latest_Code_API`, `minimal` | not present |
| UI `git status` | `?? AUDIO_CASE_AI_RUBRIC_COMPLETE_SINGLE_DOCUMENT.md` (untracked) |

Identity proof (first lines of the found file):

```
# HomeoCentrum Audio Case Taking — Combined Source of Truth
...
| Production engine | **fast-f** (`EnableFastClinicalRetrievalPipeline = true`) |
```

Full 2517-line body is **not duplicated** in this audit. The file already exists at the path above. Inlining it would make this diagnostic unreadable and would not add evidence beyond “the file exists.”

### 1.2 `fast-f` / `fastf` / `FastF` / `FAST_F` / `FastFallback` / `FastFilter` / `FastFinal`

**Code (non-docs) hits — complete list:**

| File | Line | What it does |
| ---- | ---- | ------------ |
| `Niga-Web/appsettings.json` | 141 | `"FastPipelineEngineVersion": "fast-f"` |
| `publish/appsettings.json` | 141 | same key/value in published output |
| `Niga-Domain/Configuration/RubricIntelligenceOptions.cs` | 272 | property default `= "fast-f"` |
| `Niga-Domain/Services/AudioCaseIntelligence/Orchestration/FastClinicalRetrievalOrchestrator.cs` | 66–68 | if option blank, stamp `"fast-f"`; truncates to 10 chars |

Surrounding 20 lines (orchestrator stamp):

```54:90:Niga-Domain/Services/AudioCaseIntelligence/Orchestration/FastClinicalRetrievalOrchestrator.cs
    public async Task<FastClinicalRetrievalResult> DiscoverAsync(...)
    {
        var sw = Stopwatch.StartNew();
        var startUtc = DateTime.UtcNow;
        var engineVersion = string.IsNullOrWhiteSpace(_options.FastPipelineEngineVersion)
            ? "fast-f"
            : _options.FastPipelineEngineVersion.Trim();
        if (engineVersion.Length > 10)
            engineVersion = engineVersion[..10];
        // ... speaker/negation filter, multi-query expand, parallel channels ...
```

**NOT FOUND IN CODE (`.cs` / `.json`):** `FastFallback`, `FastFilter`, `FastFinal`, `fastf`, `FastF`, `FAST_F` as identifiers.  
**Docs-only:** dozens of `fast-f` mentions under `NigaHomeopathy-API/docs/` and the untracked UI combined document.  
**UI source:** no `fast-f` in app code. Icon packs contain `mdi-fast-forward` / `la-fast-forward` (unrelated).  
**`NIGA_Latest_Code_API`:** no matches.  
**Other API git branches (`origin/main`, `origin/development`):** `git grep` for `fast-f` returned **no files**. `FastClinicalRetrievalOrchestrator.cs` is **absent** on `origin/main` / `origin/development`. It first appears in commit `9400676` (2026-08-14, message “Audio case chnages”) on `GouravDev_29-6-26`.

Related **fast-pipeline keys that do exist** (not named FastFallback/Filter/Final): `EnableFastClinicalRetrievalPipeline`, `FastPipelineEngineVersion`, `FastPipelineMaxFinalRubrics`, `FastPipelineMmrLambda`, `FastPipelineMinCanonicalScore`, `FastPipelineEnableEmbeddingSearch`, `FastPipelineEmbeddingTimeoutSeconds`, `FastPipelineEnableMultiQueryBlocks`, `FastPipelineMaxExtraQueriesPerConcept`, `FastPipelineEnableCatalogLookup`, `FastPipelineEnableQueryEmbeddingCache`, `FastPipelineCandidateFunnelSize`, `FastPipelineMinEvidenceScore`, `FastPipelineEnableDoctorLearning`, `FastPipelineSemanticCacheMaxWaitSeconds` — `Niga-Web/appsettings.json` 138–152 and `RubricIntelligenceOptions.cs` 255–304.

Default on the result DTO (overwritten at runtime by the option): `FastClinicalRetrievalResult.EngineVersion = "fast-e"` at `IFastClinicalRetrievalOrchestrator.cs:19`.

### 1.3 Which branch is built on `https://api1.homeocentrum.com/api`

**How determined:**

| Check | Result |
| ----- | ------ |
| `.github/`, `azure-pipelines*`, `Jenkinsfile`, `bitbucket-pipelines.yml`, `.gitlab-ci.yml` | **NOT FOUND IN CODE** (API repo, depth 3 + glob) |
| `appsettings.Production.json` | **NOT FOUND IN CODE** (working tree + `git log --all -- '*appsettings.Production*'`) |
| `publish/web.config` | IIS in-process: `AspNetCoreModuleV2`, `arguments=".\Niga-Web.dll"` (`publish/web.config:8`) |
| UI `src/config.js:14` | active `API_URL_NIGAHOMEOPATHY: "https://api1.homeocentrum.com/api"` |
| Anonymous `GET /api/AudioCaseIntelligence/health` (live, 200) | `engineVersion:"v2"`, `v2Enabled:true`, `rollbackToV1Only:false`, `hasRuntimeOverride:false`, `repertoryMapping.mappedRubricCount:48747`, `goldCaseCount:6` |
| Anonymous `GET /api/AudioCaseIntelligence/v3/health` (live, 200) | `v3Enabled:true`, `engineVersion:"v3.5"` — route is `api/AudioCaseIntelligence/v3/health` (`AudioCaseIntelligenceV3Controller.cs:11,96-127`), **not** `AudioCaseIntelligenceV3/health` (that 404s) |
| Live swagger `/swagger/v1/swagger.json` (200, 300 paths) | includes Audio Case + v3/v6/v7 benchmark routes; schema `AudioCaseSuggestedRubricModel` includes `canonicalScore`, `isDbBacked`, `discoveryMethod`, `hierarchyPath` |
| Those DTO fields in git | added in `9400676` (2026-08-14) on `GouravDev_29-6-26`; **absent** from `origin/main` `AudioCaseTakingModels.cs` |
| Live swagger string `fast-f` / `EnableFastClinical` | **0 occurrences** (health/config APIs do not expose the fast-pipeline flag) |
| IIS/git SHA on the server | **NOT DETERMINED** — no deploy pipeline, no version header, `HEAD` request to health returned HTTP/2 405 |
| SQL from this Mac to `localhost` / commented production host | connection refused / unreachable — **cannot read live `AudioCaseSession` rows from here** |

**What can be stated without guessing the IIS folder’s git SHA:**

- Live api1 is an ASP.NET Core site (`x-powered-by: ASP.NET`, Cloudflare).
- Live OpenAPI includes Audio Case DTOs that exist only after `9400676` on `GouravDev_29-6-26`, not on `origin/main`.
- Live `/health` **cannot** confirm `EnableFastClinicalRetrievalPipeline`, because `AudioCaseIntelligenceController.GetHealth` hard-codes `EngineVersion = config.IsV2Active ? "v2" : "v1"` (`AudioCaseIntelligenceController.cs:74`) and `RubricIntelligenceConfigModel` has **no** FastPipeline properties (`RubricIntelligenceConfigModels.cs:3-28`; live swagger matches).
- Local checkout being audited is `GouravDev_29-6-26` @ `7dc1148`. Whether IIS has that exact commit vs an earlier `9400676+` build is **NOT DETERMINED**.

### 1.4 Engine class files on the audited branch (`GouravDev_29-6-26`)

These files exist in the current API branch. Live swagger proving v3/v6/v7 **endpoints** exist on api1 does **not** prove each class runs on a given request (see §2).

**Orchestrators**

| File | One-line description (from the type itself) |
| ---- | ------------------------------------------- |
| `.../Orchestration/FastClinicalRetrievalOrchestrator.cs` | Production fast path: parallel V1/FTS/alias/embedding/catalog → gates → canonical score → MMR. |
| `.../Orchestration/IFastClinicalRetrievalOrchestrator.cs` | Interface + `FastClinicalRetrievalResult` (default stamp `"fast-e"`). |
| `.../Orchestration/RubricIntelligenceOrchestrator.cs` | V2 orchestrator: case understanding, hybrid retrieval, inference, explainability. |
| `.../V3/Orchestration/ConceptGraphOrchestrator.cs` | V3/V3.5/V6/V7 concept-graph pipeline; can call V6 or V7 when those flags are on. |
| `.../RepertoryIntelligence/RepertoryIntelligenceOrchestrator.cs` | V7 repertory intelligence discover/finalize. |
| `.../ECI/V8/EciV8ClinicalIntelligenceOrchestrator.cs` | ECI v8 end-to-end orchestrator (parser → extract → validate → DB intelligence). |
| `.../KnowledgeGraph/KnowledgeGraphOrchestratorBridge.cs` | Bridge from audio/V3 path into enterprise knowledge-graph engines. |

**V3**

| File | Description |
| ---- | ----------- |
| `.../V3/Engines/PatientMeaningGraphEngine.cs` | Builds patient-meaning graph from transcript/extraction. |
| `.../V3/Engines/MultiConceptDiscoveryEngine.cs` | Multi-concept discovery over the graph. |
| `.../V3/Engines/RubricCandidateEngine.cs` | Scores rubric candidates from graph + search. |
| `.../V3/Engines/RubricDiscoveryEngineV3.cs` | V3 M5 rubric discovery (`ModelId = "v3-m5"`). |
| `.../V3/Engines/ConceptGraphAiEngines.cs` | Concept-interpretation helpers/edge types. |
| `.../V3/Engines/ConceptGraphV35Engines.cs` | V3.5 recall/tier helpers. |

**V6**

| File | Description |
| ---- | ----------- |
| `.../V6/V6ClinicalReasoningEngine.cs` | V6 clinical reasoning discover/finalize (`EngineVersion` DTO default `v6.0`). |
| `.../V6/SqlAuthoritativeRepertorySearchEngine.cs` | SQL-authoritative repertory search for V6. |
| `.../V6/V6PerSymptomDiscoveryPipeline.cs` | Per-symptom discovery pipeline. |
| `.../V6/V6EvidenceRanker.cs` | Ranks V6 evidence. |
| `.../V6/V6SymptomOntologyBuilder.cs` | Builds V6 symptom ontology. |
| `.../V6/V6BenchmarkEvaluationService.cs` | V6 benchmark runner. |

**V7**

| File | Description |
| ---- | ----------- |
| `.../RepertoryIntelligence/RepertoryIntelligenceOrchestrator.cs` | V7 orchestrator (listed above). |
| `.../RepertoryIntelligence/Vocabulary/HomeopathicVocabularyEngine.cs` | Vocabulary expansion. |
| `.../RepertoryIntelligence/Synonyms/SynonymEngine.cs` | Synonym expansion. |
| `.../RepertoryIntelligence/Ontology/OntologyEngine.cs` | Ontology matching. |
| `.../RepertoryIntelligence/Completion/RubricCompletionEngine.cs` | Completion/retry of missing repertory coverage. |
| `.../RepertoryIntelligence/Validation/V7AccuracyBenchmarkService.cs` | V7 accuracy benchmark. |
| `.../RepertoryIntelligence/Search/CandidateRubricSearchService.cs` | Candidate search used by V7. |

**ECI v8**

| File | Description |
| ---- | ----------- |
| `.../ECI/V8/EciV8ClinicalIntelligenceOrchestrator.cs` | ECI v8 orchestrator. |
| `.../ECI/V8/Database/EciDatabaseIntelligenceEngine.cs` | ECI v8 SQL/ontology repertory retrieval. |
| Plus modules under `ECI/V8/Database/Modules/` | Embedding search, candidate merger, ranker. |

**Shared / V2 engines (still compiled; used on rollback path and/or as fast-path channels)**

| File | Description |
| ---- | ----------- |
| `Engines/ConceptKeywordDiscoveryEngine.cs` | Per-concept FTS/hotspot search; `IsAiSuggested = false`, `MatchSource = "ConceptKeyword"`. |
| `Engines/RubricAliasEngine.cs` | Alias table → `SubSectionMaster` join. |
| `Engines/EmbeddingSearchEngine.cs` | Cosine search over in-memory `RubricEmbeddings` cache. |
| `Engines/HybridRetrievalEngine.cs` | V2 hybrid merge of alias+embedding+clinical+keyword. |
| `Engines/ClinicalInferenceEngine.cs` | Infers extra DB rubrics from concepts. |
| `Engines/CaseUnderstandingEngine.cs` | Extracts clinical concepts (V2). |
| `Engines/SymptomExtractionEngine.cs` | Symptom extraction helper (V2). |
| `Engines/MetaphorInterpretationEngine.cs` | Metaphor → clinical meaning. |
| `Engines/ModalityDetectionEngine.cs` | Modality detection. |
| `Engines/ConcomitantDetectionEngine.cs` | Concomitant detection. |
| `Engines/CausationDetectionEngine.cs` | Causation links. |
| `Engines/ClinicalReasoningEngine.cs` | V2 clinical reasoning. |
| `Engines/HomeopathicReasoningEngine.cs` | V2 homeopathic reasoning. |
| `Engines/HomeopathicWeightEngine.cs` | SRP/weight application. |
| `Engines/ConfidenceScoringEngine.cs` | Calibrates hybrid scores. |
| `Engines/ExplainabilityEngine.cs` | Builds explainability payload. |
| `Engines/RepertoryTierEngine.cs` | Kent/Complete tier labels. |
| `Validation/ClinicalValidationEngine.cs` | V2.1 / enterprise validation filter. |
| `Validation/RubricQualityScoringEngine.cs` | Quality score. |
| `Validation/PrimarySymptomEngine.cs` | Primary-symptom detection. |
| `Validation/HomeopathicRulesEngine.cs` | Static homeopathic rules. |
| `Enterprise/EnterpriseRubricDiscoveryEngine.cs` | Enterprise discovery (`EngineVersionLabel = "v4.0"`). |
| `Enterprise/EnterpriseRubricExpansionEngine.cs` | Expansion of enterprise candidates. |
| `Enterprise/Quality/HierarchicalRepertorySearchEngine.cs` | Hierarchical repertory search. |
| `Enterprise/Quality/EnterpriseHybridCompletionEngine.cs` | Hybrid completion (DB + AI concepts). |
| `Enterprise/Quality/EnterpriseRubricConfidenceEngine.cs` | Enterprise confidence. |
| `Learning/DoctorFeedbackLearningEngine.cs` | Accept/reject learning weights. |
| `Embeddings/FastClinicalRubricCatalog.cs` | Process-level token catalog from embedding cache. |

`AudioCaseTakingService` does **not** reference `EnableEciV8Engine` (grep: no hits). ECI v8 is registered in DI (`ApplicationServiceExtensions.cs:196`) but is **not** on the Audio Case match call chain in that service.

---

## 2. Rubric-matching call chain (ordered, with file:line)

### 2.1 Ordered path: doctor clicks Analyze → rubrics on screen

1. **UI** `AudioCasePanel.handleAnalyze` — `NigaHomeopathy-UI/src/Components/CaseTaking/AudioCasePanel.js:469-494`  
   Dispatches `uploadAndAnalyzeAudioCase`. Config flag: none (button gated by `canAnalyze` at line 235: blob + consent + not loading).

2. **UI** `uploadAndAnalyzeAudioCase` — `src/slices/doctor/audioCaseTaking/thunk.js:133-188`  
   `POST` multipart via `uploadAudioCaseTaking` (`realbackend_helper.js:516-517`) to `/AudioCaseTaking/upload` (`url_helper.js:561`). Base URL `https://api1.homeocentrum.com/api` (`config.js:14`). No SQL/OpenAI.

3. **API** `AudioCaseTakingController.Upload` — `Niga-Web/Controllers/AudioCaseTakingController.cs:33-50`  
   Auth required. Calls `IAudioCaseTakingService.UploadAsync`.

4. **API** `AudioCaseTakingService.UploadAsync` — `Niga-Domain/Repositories/AudioCaseTakingService.cs:127-173`  
   Writes file under `AudioCaseTaking:StoragePath`. Inserts `AudioCaseSession` (Status=`Uploaded`) + `AudioCaseConsentLog`. **Tables:** `AudioCaseSession`, `AudioCaseConsentLog`, `AudioCaseSessionEventLog`. Enqueues `AudioCaseTakingJob` at line 171.

5. **API** `AudioCaseTakingBackgroundService.ExecuteAsync` — `Niga-Domain/Services/AudioCaseTakingBackgroundService.cs:60-71`  
   Single in-memory channel reader. Optional semantic-cache wait skipped when `EnableFastClinicalRetrievalPipeline` and `FastPipelineSemanticCacheMaxWaitSeconds <= 0` (lines 102-110). Then `IAudioCaseTakingService.ProcessSessionAsync`.

6. **API** `ProcessSessionAsync` — `AudioCaseTakingService.cs:457-520`  
   Progress `Processing` → `Transcribing`.

7. **OpenAI Whisper** `AudioCaseAiProcessor.TranslateAudioToEnglishAsync` — `AudioCaseAiProcessor.cs:42,66`  
   HTTP `POST audio/translations`, model `whisper-1`. Runs when `AudioCaseTaking:OutputEnglishOnly` is true (`ProcessSessionAsync` 481-491). Else `TranscribeAsync`. Result stored `AudioCaseSession.TranscriptRaw` (line 515). **Table:** `AudioCaseAiRequestLog` via `LogAiRequestAsync` (serviceType `"Translation"`).

8. **API** `RunExtractionAndRubricsAsync` — `AudioCaseTakingService.cs:764-837`

9. **OpenAI GPT** `ExtractCaseDataAsync` — `AudioCaseAiProcessor.cs:159-253`  
   HTTP `POST chat/completions`, model from `OpenAiOptions.ChatModel` (configured `gpt-4o` in appsettings; logged as `"gpt-4o"` at service 774). Prompt: system text at `AudioCaseAiProcessor.cs:176-208` (English JSON: conversation, symptoms, summary). **No rubric names in this prompt.** Persists `ConversationJson`, `SummaryJson`, `ExtractedSymptomsJson` (service 793-795). **Tables:** `AudioCaseSession`, `AudioCaseAiRequestLog`.

10. **Optional dual-language Whisper** — `AudioCaseTakingService.cs:798-807`  
    If `RubricIntelligence:DualLanguageForSensationSegments` (json `true`). Built here; **not passed** into `MatchRubricsFastClinicalAsync` (signature at 1177-1183 has no `dualLanguage` parameter).

11. **API** `MatchRubricsWithIntelligenceAsync` — `AudioCaseTakingService.cs:881-904`  
    Branch:
    - If `!IsV2Active` → `MatchRubricsV1Async` only (892-896). `IsV2Active` = `EnableV2 && !RollbackToV1Only` (`RubricIntelligenceSettingsService.cs:50-59`).
    - Else if `EnableFastClinicalRetrievalPipeline` → **`MatchRubricsFastClinicalAsync` and return** (899-903). **V3/V7/V2 orchestrator skipped.**
    - Else V3 `ConceptGraphOrchestrator.AnalyzeAsync` (909-920) and/or V2 `RubricIntelligenceOrchestrator.AnalyzeAsync` (1095-1102), merged by `RubricResultMerger` (1108).

12. **Fast path (current git config)** `MatchRubricsFastClinicalAsync` — `AudioCaseTakingService.cs:1177-1271`  
    Calls `_fastClinicalRetrieval.DiscoverAsync` (1192-1205), passing `v1Discovery: ct => MatchRubricsV1Async(..., persistMatchLogs: false)`.

13. **`FastClinicalRetrievalOrchestrator.DiscoverAsync`** — `FastClinicalRetrievalOrchestrator.cs:54-168`  
    Parallel `Task.WhenAll` (104):
    - V1 callback (`MatchRubricsV1Async`)
    - `ConceptKeywordDiscoveryEngine.DiscoverAsync`
    - `RubricAliasEngine.SearchRubricsAsync`
    - `EmbeddingSearchEngine.SearchAsync` (if `EnableEmbeddingSearch && FastPipelineEnableEmbeddingSearch`, timeout `FastPipelineEmbeddingTimeoutSeconds`)
    - catalog `LookupByTokens`  
    Merge: `RubricResultMerger.Merge` (127-130) keyed by `id:{SubSectionId}` or `name:{name}` (`RubricResultMerger.cs:166-173`).  
    Then `merged.Where(r => r.SubSectionId > 0)` (**line 132**).  
    Gates: `RubricCandidateQualityGate.Apply`, gender, hierarchy, `ApplyHallucinationHardGate` (min evidence `FastPipelineMinEvidenceScore`).  
    Rank: `FastClinicalRanking.ApplyCanonicalScores` + optional doctor-learning boost + `SelectWithMmr` (`FastPipelineMmrLambda`, `FastPipelineMinCanonicalScore`, cap `FastPipelineMaxFinalRubrics`).  
    **SQL:** V1 + keyword use `SearchSubSectionsByHotspotAsync` (`SubSectionRepository.cs:294-325`): `CONTAINS(s.SubSectionName, {0})` on `dbo.SubSectionMaster` where `DeleteStatus = 0`. Alias: join `RubricAlias` × `SubSectionMaster` (`RubricIntelligenceAdminService.cs:376-380`). Embedding: `RubricEmbeddings` ⋈ `SubSectionMaster` at cache load (`RubricEmbeddingRepository.cs:47-51`). Catalog: in-memory snapshot of that cache (`FastClinicalRubricCatalog.cs:55-80,212-239`). **OpenAI:** embedding vectors for query texts if cache miss (`EmbeddingSearchEngine.cs:70-80`).

14. **V1 nested inside fast path** `MatchRubricsV1Async` — `AudioCaseTakingService.cs:1645-1751`  
    Per symptom search terms, hotspot FTS, `BuildRubric` (`1845-1856`) with `IsAiSuggested = false`, `MatchSource = "Database"`.  
    Then **if `EnableAiSuggestedRubrics`** (1716-1741): GPT `SuggestAiRubricsAsync`. **OpenAI** `chat/completions` (`AudioCaseAiProcessor.cs:340-396`). System prompt **explicitly** says names “may not exist in the user's database” (lines 340-343). `MapAiRubrics` assigns **negative** `SubSectionId` starting at `-1` (441-462).  
    Fast orchestrator then **drops** `SubSectionId <= 0` (orchestrator 132 and service 1247-1248), so these GPT rows normally never reach JSON.

15. **Post-fast validation** — `AudioCaseTakingService.cs:1212-1244`  
    If `RequiresStrictValidation` (`EnableEnterpriseClinicalValidation || EnableClinicalValidationV21`, options 327-328). Both json flags are `true`, so `ClinicalValidationEngine.ValidateAndFilter` runs. No extra SQL/OpenAI.

16. **`FinalizeRubricsForResponseAsync`** — `AudioCaseTakingService.cs:1255-1261, 1274-1337`  
    Always: `AiSuggestedRubricReconciler.ReconcileAsync` (1282-1285) at threshold `AiReconciliationMinConfidence` (json `0.7`). Then `RubricCandidateQualityGate.Apply`, `RubricUnifiedContractHelper.ApplyUnifiedContract`, optional evidence-chain complete filter, **`PersistRubricMatchLogsAsync`** (1305) → table `AudioCaseRubricMatchLog`, `RubricModalityVariantGrouper.GroupForDisplay`, take 20. If `IsV3Active`, also `SaveDisplayedRubricsAsync` (1315-1319) even on the fast path (V3 flag is true in json).

17. **Assemble and save `SuggestedRubricsJson`** — `AudioCaseTakingService.RunExtractionAndRubricsAsync` **lines 858-868**  
    `session.SuggestedRubricsJson = JsonSerializer.Serialize(rubrics, JsonOptions);` then `Status = "Completed"`, `SaveChangesAsync`. **This is the exact assembly point.**

18. **UI poll** `pollAudioCaseAnalysis` — `thunk.js:216-257`, interval `POLL_INTERVAL_MS = 2500` (thunk.js:36).  
    `GET /AudioCaseTaking/{id}/status` (`AudioCaseTakingController.cs:70`; service `GetStatusAsync` 378-397). `EngineVersion` from `session.IntelligenceEngineVersion ?? ConceptGraphEngineVersion` (393). Fast path writes both to `fast.EngineVersion` (service 1207-1209).

19. **UI result** `GET /AudioCaseTaking/{id}/result` — controller 92-99; `GetResultAsync` 429-439; **`MapResult` 2050-2072** deserializes `SuggestedRubricsJson` **with no join back to `SubSectionMaster`**.

20. **UI render** `dispatchCompletedAnalysis` → reducer `suggestedRubrics` (`thunk.js:75-88`, `reducer.js:132`) → `AudioCasePanel.js:1273-1274` → `AudioCaseRubricSuggestions`.

### 2.2 Which engine(s) execute for a real request today

**From git config on this branch / `publish/appsettings.json` (identical RubricIntelligence block):**  
`EnableFastClinicalRetrievalPipeline: true` → the `if` at `AudioCaseTakingService.cs:900-903` takes the fast path. Engines that **execute**: `FastClinicalRetrievalOrchestrator` + nested `MatchRubricsV1Async` + `ConceptKeywordDiscoveryEngine` + `RubricAliasEngine` + `EmbeddingSearchEngine` (if both embedding flags true) + `FastClinicalRubricCatalog` + `ClinicalValidationEngine` + `AiSuggestedRubricReconciler`.  
Engines that **do not execute** on that branch of the `if`: `ConceptGraphOrchestrator`, `RubricIntelligenceOrchestrator`, V6, V7, ECI v8.

**From live api1:** flags `EnableV2` and `EnableV3ConceptGraph` are true (health/v3 health). The fast-pipeline flag is **not exposed**. Existing timing instrumentation to confirm the path without guessing:

- Session columns `IntelligenceEngineVersion` / `ConceptGraphEngineVersion` / `RecallEngineVersion` (set to `fast.EngineVersion` at service 1207-1209). Status API returns that string (393).
- `AudioCaseIntelligenceLog` stage `"FastClinicalRetrieval"` (orchestrator 195-198).
- `GET result` `ProcessingMetrics` loaded from intelligence log where `StageName` = summary and `EngineVersion` = `"base-a"` (`TryLoadProcessingMetricsAsync` 1962-1974; constant `RubricPipelineTelemetryModels.cs:10`).
- Debugger breakpoint: `AudioCaseTakingService.cs:900` (`EnableFastClinicalRetrievalPipeline`) and `1258` (`finalized` return). No temporary logging was added (this pass is report-only).

**Live confirmation of last-N session engine stamps: NOT DETERMINED** (no SQL from this machine; result API is authenticated).

### 2.3 Orchestrator vs independent writers

There **is** a single merge class on the fast path: `FastClinicalRetrievalOrchestrator` (`FastClinicalRetrievalOrchestrator.cs:127-130`) using `RubricResultMerger.Merge`.  
On the **legacy** path, `AudioCaseTakingService.MatchRubricsWithIntelligenceAsync` 1107-1110 also uses `RubricResultMerger.Merge` (V1 + V2 + optional V3 supplement).  
Multiple engines do **not** independently persist `SuggestedRubricsJson`. Only `RunExtractionAndRubricsAsync` line 858 writes that column. Match logs are written once in `FinalizeRubricsForResponseAsync` → `PersistRubricMatchLogsAsync` (1305, 1754-1805).

### 2.4 Where `SuggestedRubricsJson` is assembled

`AudioCaseTakingService.RunExtractionAndRubricsAsync`, **line 858**:

```858:868:Niga-Domain/Repositories/AudioCaseTakingService.cs
        session.SuggestedRubricsJson = JsonSerializer.Serialize(rubrics, JsonOptions);
        await LogEventAsync(sessionId, correlationId, "LatencyGap3_SuggestedRubricsJsonWritten", "Success",
            $"SuggestedRubricsJson length={session.SuggestedRubricsJson?.Length ?? 0}");

        session.Status = "Completed";
        session.CurrentStep = "Completed";
        session.CompletedAtUtc = DateTime.UtcNow;
```

`rubrics` is the return value of `MatchRubricsWithIntelligenceAsync` (830-837).

---

## 3. Root cause analysis: why non-DB rubrics are displayed (with evidence)

### 3.1 Every assignment of `IsAiSuggested = true` / `MatchSource = "AiGenerated"` (and close variants)

| File:line | Assignment |
| --------- | ---------- |
| `AudioCaseAiProcessor.cs:453-460` | `SubSectionId = aiId` (negative), `IsAiSuggested = true`, `MatchSource = "AiGenerated"` (`MapAiRubrics`) |
| `AudioCaseAiProcessor.cs:478-484` | mock: `IsAiSuggested = true`, `MatchSource = "AiGenerated"` (`BuildMockAiRubrics`) |
| `EnterpriseRubricPresentationHelper.cs:35-39` | `SubSectionId = 0`, `IsAiSuggested = true`, `MatchSource = "AiClinicalConcept"` |
| `ConceptGraphOrchestrator.cs:1280` | `IsAiSuggested = true` on a model that copies `candidate.SubSectionId` (can be **positive**) |
| `ConceptGraphOrchestrator.cs:1467` | `IsAiSuggested = !isRepertoryDb` (`isRepertoryDb` iff `DiscoveryMethod == RepertoryDb`, line 1448) |
| `AiSuggestedRubricReconciler.cs:75` | if reconcile **fails**: `IsAiSuggested = true` |
| `HybridRetrievalEngine.cs:108` | `IsAiSuggested = aliasRubric?.IsAiSuggested ?? embeddingScore >= 0.7m` on a **positive** `SubSectionId` (line 96) |
| Tests only | `Tasks4to7AccuracyTests.cs:66`, `CorrectnessBugsAtoDTests.cs:177,247` |

`PersistRubricMatchLogsAsync` 1788 sets `MatchSource = ... "AiGenerated"` when `IsAiSuggested` if `MatchSource` was null. The **entity has no `IsAiSuggested` column** (`AudioCaseRubricMatchLog.cs` 1-58).

`AiSuggestedRubricReconciler.cs:58-60` **clears** AI flags on success: `IsAiSuggested = false`, `MatchSource = "AiReconciled"`, **positive** `SubSectionId` from `SubSectionMaster`.

### 3.2 Negative vs positive `SubSectionId` for AI names

**Convention followed in `MapAiRubrics` / `BuildMockAiRubrics`:** negative IDs (`aiId = -1` then `--`) — `AudioCaseAiProcessor.cs:448-462, 473-486`.

**Convention NOT followed (positive or zero IDs on AI-labelled rows):**

| Path | ID | Flag | Live on fast-f git path? |
| ---- | -- | ---- | ------------------------ |
| `EnterpriseRubricPresentationHelper.CreateAiClinicalConcept` | **0** | `IsAiSuggested=true` | Not constructed by fast orchestrator |
| `ConceptGraphOrchestrator` ~1265-1280 | **candidate ID, often > 0** | **always** `IsAiSuggested=true` | Skipped when fast flag true |
| `ConceptGraphOrchestrator` 1455-1467 | discovery ID | true unless `RepertoryDb` | Skipped when fast flag true |
| `HybridRetrievalEngine.cs:96,108` | **positive DB id** | true if embedding cosine ≥ 0.7 | Skipped when fast flag true |
| `AiSuggestedRubricReconciler.cs:55-60` | **positive** `match.SubSectionId` | **false**, `MatchSource=AiReconciled` | **Does run** on fast path (`FinalizeRubricsForResponseAsync` 1282) |

**Likely mechanism if the doctor sees a name that is not in `SubSectionMaster` but looks like a normal repertory row:**

1. **GET does not re-validate names** (`MapResult` 2057-2072). The UI prints `rubric.subSectionName` from JSON (`AudioCaseRubricSuggestions.js:169`) after `formatRubricTitle` splits on `-` (lines 14-20). There is **no** client call to `SubSectionMaster`.
2. **`IsDbBacked` is not a live FK check.** `FastClinicalEvidenceGate` sets `IsDbBacked = r.SubSectionId > 0` (gate file ~164, 199). Comment on the DTO (`AudioCaseTakingModels.cs:192-193`) says it means the ID maps to `SubSectionMaster`; the assignment does not query the table.
3. **Reconciler can promote a GPT free-text name to a positive ID** using exact name **or** token-overlap / `Contains` scoring (`AiSuggestedRubricReconciler.cs:98-176, 210-225`). On success it **overwrites** `SubSectionName` with the matched master name (line 56). On **failure** it keeps the GPT name and `IsAiSuggested=true`. Fast path usually already dropped `SubSectionId<=0` **before** finalize (1247-1248), so promotion of leftover GPT rows is limited unless a GPT name somehow already had `SubSectionId>0` (MapAiRubrics does not do that).
4. **If live IIS has `EnableFastClinicalRetrievalPipeline=false`** (flag not visible on health), V1 GPT rows with negative IDs **would** reach finalize, then either stay as non-DB names or be fuzzy-mapped onto a **different** real `SubSectionMaster` row (`MatchSource=AiReconciled`, `IsAiSuggested=false`) — UI would treat them as repertory (`subSectionId > 0`).
5. **Stale embedding/catalog names:** cache load joins live `SubSectionMaster` (`RubricEmbeddingRepository.cs:47-51`). After process start, names are frozen in memory (`FastClinicalRubricCatalog` snapshot). A later rename/delete in SQL would not update the in-process snapshot. **Whether that happened: NOT DETERMINED** (no SQL).

**Positive ID without existing master row** is possible if an embedding/alias row’s `RubricId`/`SubSectionId` is orphaned: alias search inner-joins master (`RubricIntelligenceAdminService.cs:376-379`); embedding load also inner-joins. Catalog only indexes cache entries with `RubricId > 0` (`FastClinicalRubricCatalog.cs:78-79`). Orphans in those channels are **filtered at load**, not at GET.

### 3.3 Frontend render and badges

Primary list: `AudioCaseRubricSuggestions.js`, mounted from `AudioCasePanel.js:1273-1286`.

Classification (`AudioCaseRubricSuggestions.js:67-70` + helper `audioCaseTakingHelper.js:64-66`):

```javascript
// helper
isAiClinicalConceptOnly = resultKind === 'aiclinicalconcept'
  || (isAiSuggested && !(subSectionId > 0))

// component
repertoryRubrics  = !isAiClinicalConceptOnly && subSectionId > 0
aiConceptRubrics  = isAiClinicalConceptOnly
legacyAiRubrics   = isAiSuggested && !isAiClinicalConceptOnly && subSectionId > 0
```

Visual distinction:

- AI concept / `subSectionId<=0`: badges **“AI Clinical Concept”** and **“Not in repertory”** (`AudioCaseRubricSuggestions.js:170-178`).
- `isAiSuggested && !isAiConcept`: badge **“AI suggested”** (180-184).
- Repertory rows (`subSectionId>0` and not concept-only): **no** “not in DB” badge even if the name is absent from `SubSectionMaster`.
- Explainability / confidence UI only if `engineVersion` is `v2|v4.0|v5.2|v6.0|v7.0` (line 67) — **not** `fast-f`. On a fast-f session those extra badges/panels are hidden; the name still renders.

Apply-to-board: `mapSuggestedRubricToRepertorization` returns `null` if `resultKind` is AI concept **or** `!(subSectionId > 0)` (`audioCaseTakingHelper.js:48-50`). Positive IDs are applied using the JSON name, not a fresh master lookup.

### 3.4 Are `EnableAiSuggestedRubrics` / `MaxAiSuggestedRubrics` respected?

Exact condition (`AudioCaseTakingService.cs:1716-1725`):

```
if EnableAiSuggestedRubrics:
  aiSlots = min(MaxAiSuggestedRubrics, max(0, 20 - dbTop.Count))
  if dbTop.Count == 0: aiSlots = MaxAiSuggestedRubrics
  if aiSlots > 0: call SuggestAiRubricsAsync
```

This is **not** “DB zero/low then fallback.” Any `dbTop.Count < 20` opens GPT slots. With json `MaxAiSuggestedRubrics=25` and 12 DB hits, `aiSlots = min(25, 8) = 8`. Combined list is then `.Take(20)` (1744).  
On fast path those GPT rows are typically discarded by `SubSectionId > 0` (orchestrator 132, service 1248). The GPT call still runs inside V1 because `persistMatchLogs: false` does not skip suggestions.

### 3.5 GPT free-text shown without `SubSectionMaster` validation

Quoted path:

```340:343:Niga-Domain/Services/AudioCaseAiProcessor.cs
            You are a homeopathic repertory assistant. Suggest English rubric names in standard repertory style
            ...
            These are AI suggestions only — they may not exist in the user's database.
```

```441:461:Niga-Domain/Services/AudioCaseAiProcessor.cs
    private static List<AudioCaseSuggestedRubricModel> MapAiRubrics(...)
    {
        var aiId = -1;
        foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i.RubricName)).Take(maxCount))
        {
            result.Add(new AudioCaseSuggestedRubricModel
            {
                SubSectionId = aiId,
                SubSectionName = item.RubricName.Trim(),  // GPT string, no DB lookup
                ...
                IsAiSuggested = true,
                MatchSource = "AiGenerated",
            });
            aiId--;
```

`CreateAiClinicalConcept` uses `homeo.ConceptName` with `SubSectionId = 0` (`EnterpriseRubricPresentationHelper.cs:32-39`) — also not a master lookup.

`MapResult` never joins master (`AudioCaseTakingService.cs:2057-2072`).

### 3.6 Cross-check of real `AudioCaseRubricMatchLog` / `SuggestedRubricsJson` vs `SubSectionMaster`

**NOT DETERMINED FROM THIS MACHINE.**

Attempts:

- `sqlcmd`: not installed.
- `pymssql` to `localhost` / `127.0.0.1`: connection refused.
- `pymssql` to the host in the commented `DefaultConnection`: network unreachable.
- `GET /api/AiMonitoringDashboard/embedding-health`: HTTP 401 (auth required).
- `GET result` / session list: `[Authorize]` (`AudioCaseTakingController.cs:16`).

No PHI rows were retrieved. Counts in §6 from live **anonymous health** are repertory-**mapping** counts, not `SubSectionMaster` row counts.

---

## 4. Config reality table (documented / option default vs files that exist)

`appsettings.Production.json`: **NOT FOUND IN CODE.**  
Sources used: `Niga-Web/appsettings.json` (and identical Audio/Rubric blocks in `publish/appsettings.json`). `appsettings.Development.json` has **no** `AudioCaseTaking` / `RubricIntelligence` sections (falls through to base file when both are loaded).

Live anonymous health confirms **subset**: `EnableV2` effective true, `RollbackToV1Only` false, `EnableRepertoryMapping` true, `EnableV3ConceptGraph` effective true (`v3Enabled`), `RequireManualApprovalForAllAiRubrics` effective true. Other keys below are **from git/publish JSON**, not proven on IIS.

| Key | In `Niga-Web/appsettings.json` | C# default if key missing | Live anonymous API |
| --- | ------------------------------ | ------------------------- | ------------------ |
| **AudioCaseTaking** | | `AudioCaseTakingOptions.cs` | |
| MaxFileSizeBytes | 52428800 (line 29) | 52_428_800 (line 9) | not exposed |
| MaxAudioDurationMinutes | 45 (30) | 45 (11) | not exposed |
| UseMockWhenNoApiKey | false (32) | **true** (15) | not exposed |
| EnableSemanticRubricMatch | true (33) | true (17) | not exposed |
| OutputEnglishOnly | true (34) | true (20) | not exposed |
| EnableAiSuggestedRubrics | true (35) | true (23) | not exposed |
| MaxAiSuggestedRubrics | **25** (36) | **10** (25) | not exposed |
| MaxProcessingMinutes | **20** (39) | **10** (33) | not exposed |
| **RubricIntelligence** | | `RubricIntelligenceOptions.cs` | |
| EnableV2 | true (48) | false (8) | health `v2Enabled:true` |
| RequireManualApprovalForAllAiRubrics | true (50) | true (14) | health `requiresManualApproval:true` |
| EnableEmbeddingSearch | true (53) | true (21) | not on health |
| HybridWeights.Embedding | 0.40 (59) | 0.40 (`HybridWeightOptions.cs:5`) | not exposed |
| HybridWeights.Alias | 0.30 (60) | 0.30 (7) | not exposed |
| HybridWeights.ClinicalMeaning | 0.20 (61) | 0.20 (9) | not exposed |
| HybridWeights.KeywordLike | 0.10 (62) | 0.10 (11) | not exposed |
| EnableClinicalInference | true (64) | true (33) | not on health |
| EnableRepertoryMapping | true (69) | true (44) | health `enableRepertoryMapping:true` |
| RollbackToV1Only | false (70) | false (47) | health `rollbackToV1Only:false` |
| EnableClinicalValidationV21 | true (71) | false (50) | not on health |
| EnableV3ConceptGraph | true (80) | false (72) | v3 health `v3Enabled:true` |
| EnableV35RecallEngine | true (89) | true (109) | v3 health `v35RecallEnabled:true` |
| EnableV35FastPipeline | true (98) | true (128) | not on health |
| EnableV6ClinicalReasoningEngine | **false** (127) | false (223) | not on health |
| EnableV7RepertoryIntelligenceEngine | true (132) | false (238) | not on health; V7 only runs if **not** on fast path (`ConceptGraphOrchestrator.cs:170,427`) |
| EnableEciV8Engine | **NOT IN JSON** | false (307) | swagger: **0** ECI paths |
| EnableFastClinicalRetrievalPipeline | true (138) | false (260) | **not exposed** |
| FastPipelineEngineVersion | `"fast-f"` (141) | `"fast-f"` (272) | **not exposed** |
| FastFallback / FastFilter / FastFinal keys | **NOT FOUND IN CODE** | n/a | n/a |

`hasRuntimeOverride:false` on live health means `RubricIntelligenceSettingsService` in-memory overrides are not set; it does **not** dump the rest of appsettings.

---

## 5. Timing reality table (per-stage, real sessions)

**Last 10–20 completed sessions from DB: NOT DETERMINED** (SQL unreachable; result API authenticated).

No temporary stopwatch was added (report-only). **Existing instrumentation already in code:**

| Source | What it stores |
| ------ | -------------- |
| `AudioCaseAiRequestLog.LatencyMs` | Whisper / GPT extraction / AiRubricSuggestion (`LogAiRequestAsync` 1859-1871) |
| `IRubricPipelineTelemetry.RecordStageAsync` | Whisper, GptExtraction, DualLanguageWhisper, MatchRubricsWithIntelligence, FastClinicalRetrieval, Finalization (`ProcessSessionAsync` 507-513; `RunExtractionAndRubricsAsync` 778-854, 872) |
| `AudioCaseSessionEventLog` | `LatencyGap3_MatchRubricsDone`, `LatencyGap3_SuggestedRubricsJsonWritten`, `LatencyGap3_StatusCompletedSaved` (839-871) |
| `GET result` `ProcessingMetrics` | deserialized from `AudioCaseIntelligenceLog` where `EngineVersion='base-a'` (1962-1979) — **can disagree** with session stamp `fast-f` |
| Status `ElapsedSeconds` | `CompletedAt`/`ChangedDate` − `EnteredDate` with local-vs-UTC guard (`GetStatusAsync` 400-426) |

**Historical measurements already in repo docs** (not this machine’s live query; source `docs/AUDIO_CASE_PERFORMANCE_AND_ACCURACY.md` citing 2026-08-13 exports). These are **not** labelled `fast-f`:

| Stage | Legacy V7 case | Fast-c case_5 |
| ----- | -------------- | ------------- |
| SemanticCacheWait | 120.0 s | 0 s |
| Whisper | 116.5 s | 139.2 s |
| GptExtraction | 11.6 s | 10.5 s |
| Match / discovery | 314.6 s | 1.9 s |
| FastClinicalRetrieval | — | 1.5 s |
| Total | 562.9 s (~9.4 min) | 151.7 s (~2.5 min) |

SQL 730-C averages in that same doc (N≈4 mixed engines): Whisper avg 115 s (min 97, max 139); GptExtraction avg 11 s; FastClinicalRetrieval 1.5–3.0 s; ProcessSessionTotal avg 207 s (min 111, max 443).  
**p50/p90/max for `fast-f` sessions: NOT DETERMINED.** Same doc states 730-D NTILE returned NULL and `fast-f` was not in the export.

Frontend poll budget (code, not DB): `MAX_POLL_ATTEMPTS = 192` × 2500 ms ≈ 8 minutes (`thunk.js:36` and poll loop 226). Backend cap: `MaxProcessingMinutes` json 20.

---

## 6. Data quality findings (table row counts, staleness)

**Direct `COUNT(*)` on `SubSectionMaster` / `RubricAlias` / `RubricMetaphorDictionary` / `RubricEmbeddings` / `AiRubricEmbedding`: NOT DETERMINED** (no SQL).

**What the live anonymous health *did* return (mapping tables, not master row counts):**

- `repertoryMapping.mappedRubricCount = 48747` (Kent 48724, Complete 23, `activeSourceCount = 2`) — `GET /api/AudioCaseIntelligence/health` 2026-08-17.
- Gold library: 6 active gold cases.
- 30-day acceptance 74.83% (n related to 7 sessions benchmarked / 14-day gates as reported by that payload).

**Code facts about those satellite tables (whether empty would silently degrade search):**

| Table | EF | Used on fast path? | If empty |
| ----- | -- | ------------------ | -------- |
| `SubSectionMaster` | `NIGACentrumContext` + FTS in `SubSectionRepository.cs:310-315` | Yes (V1 + keyword) | FTS/Contains return no DB hits |
| `RubricAlias` | `DbSet` line 149; search `RubricIntelligenceAdminService.cs:376-380` | Yes (`SafeAliasAsync`) | alias channel returns `[]` (caught → empty, orchestrator 258-273) |
| `RubricMetaphorDictionary` | `DbSet` line 147 | Metaphor engine on **V2** path; not a fast-path parallel channel | V2 metaphor degrade; built-in metaphors exist in `GetBuiltInMetaphors` (397+) if SQL throws |
| `RubricEmbeddings` | entity `RubricEmbedding.cs` (`CreatedDate`, `UpdatedDate`); load `RubricEmbeddingRepository.cs:47-51` | Yes if both embedding flags true | `EmbeddingSearchEngine` returns `Error = "No indexed rubric embeddings available."` (51-55) — channel empty, keyword/V1 still run |
| `AiRubricEmbedding` | `AiEmbeddingInfrastructureEntities.cs:40-71` (`UpdatedDate`) | Enterprise/V3 embedding infra + monitoring (`AiMonitoringDashboardService.cs:449,465`) | Fast path uses **legacy** `RubricEmbeddings` via `RubricEmbeddingMemoryCache`, not this table, unless a different cache is wired (fast `EmbeddingSearchEngine` uses `IRubricEmbeddingMemoryCache` → `RubricEmbeddingRepository.LoadAllAsync` on `RubricEmbeddings`) |

Staleness of `RubricEmbeddings.UpdatedDate` / `AiRubricEmbedding.UpdatedDate`: **NOT DETERMINED**.  
`KeepLegacyRubricEmbeddingsActive` default true (`AiEmbeddingInfrastructureOptions.cs:33-34`).

---

## 7. Open questions / things that could not be determined from code alone

1. Exact git SHA / IIS site path of `api1.homeocentrum.com` (no CI, no version endpoint, health does not report `fast-f`).
2. Live `appsettings` on the server (no `appsettings.Production.json` in git; IIS file not readable from this Mac).
3. Whether `EnableFastClinicalRetrievalPipeline` is true **on the running process**. Git/publish say true; live health cannot confirm. If false, V3 (`v3Enabled:true` on live) and V1 GPT suggestions **would** execute.
4. Last 10–20 `AudioCaseSession` engine stamps, Whisper/GPT/match durations, p50/p90/max.
5. Row counts and last-updated dates for `SubSectionMaster` (DeleteStatus=0), `RubricAlias`, `RubricMetaphorDictionary`, `RubricEmbeddings`, `AiRubricEmbedding`.
6. 3–5 recent `AudioCaseRubricMatchLog` / `SuggestedRubricsJson` names vs `SubSectionMaster` (orphans vs `AiGenerated` vs `AiReconciled` vs `Database`).
7. In-process embedding cache size and `LastRefreshedUtc` on the live worker.
8. Whether doctors are looking at **historical** sessions processed on V3/V7 (JSON names persist; GET never re-joins master).
9. `MaxAudioDurationMinutes` is bound on `AudioCaseTakingOptions.cs:11` and set in json, but a `*.cs` grep of the API repo found **no other reads** — duration is not enforced in code. (Not an open question; listed here because it was not part of sections 1–6.)
10. Whether any hosted job other than Audio Case calls ECI v8. ECI is in DI (`ApplicationServiceExtensions.cs:196`) and **not** referenced from `AudioCaseTakingService`; live swagger has 0 ECI routes. Other callers: not fully traced.

---

*End of fact-finding audit. No recommendations in this pass.*

# AI Rubric Engine — Performance Benchmark

**Status:** Early averages from SQL 730-C (case_6.xlsx). P50/P90 (730-D) returned NULL — **N too small for NTILE**.

**Source:** `NigaHomeopathy-UI/Result for the Ai case_6.xlsx`

## Targets

| Metric | Target | Measured |
|---|---|---|
| P50 total | ≤ 120 s | **TBD** (730-D NULL; N≈4) |
| P90 total | ≤ 180 s | **TBD** |
| Rubric intelligence post-Whisper | ≪ minutes | FastClinicalRetrieval **~1.5–3.0 s** |
| LLM calls | 1 extract | prior case_5: **1** |

## Stage averages (SQL 730-C)

| Stage | EngineVersion | N | AvgMs | MinMs | MaxMs |
|---|---|---|---|---|---|
| FastClinicalRetrieval | base-a | 3 | 2411 | 1521 | 3039 |
| FastClinicalRetrieval | fast-c | 1 | 1521 | 1521 | 1521 |
| FastClinicalRetrieval | fast-d | 1 | 3039 | 3039 | 3039 |
| FastClinicalRetrieval | fast-e | 1 | 2673 | 2673 | 2673 |
| GptExtraction | base-a | 4 | 11161 | 10475 | 11645 |
| MatchRubricsWithIntelligence | base-a | 4 | 80702 | 1880 | 314584 |
| ProcessSessionTotal | base-a | 4 | 207186 | 110907 | 442845 |
| PipelineBaselineSummary | base-a | 4 | 237249 | 110964 | 562918 |
| SemanticCacheWait | base-a | 4 | 30003 | 0 | 120015 |
| Whisper | base-a | 4 | 115207 | 96755 | 139161 |

### How to read this

- **FastClinicalRetrieval ~1.5–3 s** — rubric path is healthy.  
- **Whisper ~96–139 s** — dominates wall clock.  
- **MatchRubrics Max 314 s** — legacy/mixed path in older `base-a` rows; fast path Min **1880 ms**.  
- **SemanticCacheWait Max 120 s** — old timeout; fast path should show **0**.  
- **fast-f** not in this export yet (post-deploy upload will create it).

## Fleet P50/P90 (SQL 730-D)

All ApproxP50/P90 were **NULL** (N&lt;~100 per bucket). Re-run after more production sessions.

## Conclusion

Rubric intelligence meets speed goal on measured fast rows. End-to-end ≤2–3 min depends on audio length (Whisper). Collect `fast-f` sessions from your upcoming frontend tests.

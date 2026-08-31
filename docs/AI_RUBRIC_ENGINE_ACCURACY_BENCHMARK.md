# AI Rubric Engine — Accuracy Benchmark

**Status:** Early measured aggregate from SQL 730 (case_6.xlsx). **N is too small for production claims.**

**Source:** `NigaHomeopathy-UI/Result for the Ai case_6.xlsx` + prior case_5 review.

## Targets (not claimed as met)

| Metric | Target | Preferred | Measured |
|---|---|---|---|
| Doctor Acceptance Rate | ≥ 90% | ≥ 95% | see table below (tiny N) |
| Precision@10 | ≥ 90% | ≥ 95% | **TBD** (need per-session suggested vs accepted join) |
| False Positive | &lt; 5% | | **TBD** |
| DB-backed final rate | ≥ 95% | ~100% | case_5: **8/8 (100%)** (n=1) |

## Aggregate acceptance by engine (SQL 730-B / case_6)

| EngineVersion | FeedbackRows | Accepted | Rejected | AcceptanceRate |
|---|---|---|---|---|
| v2 | 14 | 7 | 7 | **0.50** |
| v7.0 | 4 | 4 | 0 | **1.00** (n=4 only) |
| fast-c / fast-d / fast-e / fast-f | — | — | — | **no feedback rows yet** |

## Golden sessions (730-A)

See `AI_RUBRIC_GOLDEN_SET.md` (3 sessions).

## Precision@10 on golden holdout

| EngineVersion | N sessions | Mean P@10 | Mean R@10 | Notes |
|---|---|---|---|---|
| | 0 measured | | | Run after `fast-f` production cases with Accept/Reject |

## Remaining gaps

- No doctor feedback yet on **fast-*** engines.  
- Need ≥50 sessions before acceptance targets are credible.  
- After your frontend upload test: Accept/Reject rubrics, then re-run 730.

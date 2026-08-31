# AI Rubric Golden Set

**Purpose:** Regression / accuracy benchmark for the fast clinical pipeline (Phases 45–46).  
**Rule:** No patient-identifying text. Store **SessionId only**.

**Source (730-A):** `NigaHomeopathy-UI/Result for the Ai case_6.xlsx` (2026-08-13)

## Current registry (from SQL 730-A)

| # | SessionId | EngineVersion | Accepted | Rejected | FeedbackRubrics | CompletedAtUtc (UTC) | Split |
|---|---|---|---|---|---|---|---|
| 1 | `D0E9EC14-3AE8-4E4C-817F-732E41B2FB84` | v7.0 | 1 | 0 | 1 | 2026-07-11 | holdout |
| 2 | `34EB3FFB-4150-431A-AC46-458F2EC2A4BA` | v7.0 | 3 | 0 | 3 | 2026-07-07 | holdout |
| 3 | `69C354E6-B6BD-4C9B-B43B-75FCCC7549BD` | v2 | 7 | 7 | 14 | 2026-06-25 | holdout |

**N = 3 sessions** — far below target 50+. Golden set status: **STARTED / INCOMPLETE**.

## How to grow

1. After each production analysis, doctor Accept/Reject rubrics.  
2. Re-run `docs/sql/730_Golden_Set_Benchmark_Queries.sql` section A.  
3. Prefer new rows with `IntelligenceEngineVersion` = `fast-f` (or current fast stamp).  
4. Target **50+** sessions before claiming Precision@10 / acceptance targets.

## Vibration / hands / fit regression (unit)

- `FastClinicalAccuracyPackTests.VibrationHandsFit_Regression_*`  
- No live SessionId required for that unit gate.

## Evaluation procedure

1. Load Accepted SubSectionIds per SessionId.  
2. Compare to ranked suggestions for that engine version.  
3. Compute Precision@10 via `RubricBenchmarkMetrics` / SQL join.  
4. Record in `AI_RUBRIC_ENGINE_ACCURACY_BENCHMARK.md`.

**Never claim ≥90% acceptance until N is large enough and measured.**

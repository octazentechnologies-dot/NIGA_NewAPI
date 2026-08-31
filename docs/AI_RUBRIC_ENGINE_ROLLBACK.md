# AI Rubric Engine — Rollback

## Fast Clinical Retrieval Pipeline

**Flag:** `RubricIntelligence:EnableFastClinicalRetrievalPipeline`

| Mode | Config | Engine stamp |
|---|---|---|
| Production fast path | `true` (default) | `fast-f` |
| Legacy V7/Enterprise | `false` | `v7.0` |

1. Set flag to `false` in `appsettings.json` (or env override).  
2. Restart API process.  
3. Confirm next session `ConceptGraphEngineVersion` is `v7.0` (not `fast-f`).  
4. Optional: keep `FastPipeline*` keys unchanged — ignored when flag is false.

No table drops / data deletes required.

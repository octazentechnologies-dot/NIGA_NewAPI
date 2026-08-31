# AI Rubric Engine — Threshold Configuration (Fast Pipeline)

Production defaults (`appsettings.json` / `RubricIntelligence`):

| Key | Default | Meaning |
|---|---|---|
| `EnableFastClinicalRetrievalPipeline` | **true** | Production fast path on |
| `FastPipelineSemanticCacheMaxWaitSeconds` | 0 | Never block on cold cache |
| `FastPipelineMaxFinalRubrics` | 12 | Cap; MMR may return fewer |
| `FastPipelineEngineVersion` | `fast-f` | Stamp on rubrics/session |
| `FastPipelineEnableEmbeddingSearch` | true | Parallel embedding channel |
| `FastPipelineEmbeddingTimeoutSeconds` | 8 | Skip embeddings if slow |
| `FastPipelineMmrLambda` | 0.80 | MMR relevance weight |
| `FastPipelineMinCanonicalScore` | 0.45 | Floor before MMR |
| `FastPipelineEnableMultiQueryBlocks` | true | Sensation+location query expand |
| `FastPipelineMaxExtraQueriesPerConcept` | 4 | Cap extra queries |
| `FastPipelineEnableCatalogLookup` | true | Process-level token funnel |
| `FastPipelineEnableQueryEmbeddingCache` | true | Cache query vectors |
| `FastPipelineCandidateFunnelSize` | 80 | Merge funnel before gates |
| `FastPipelineMinEvidenceScore` | 0.15 | Hallucination hard gate floor |
| `FastPipelineEnableDoctorLearning` | true | Soft ranking boost only |
| `EnableV7…` / `EnableEnterprise…` | true | Kept for rollback path only |

## Canonical score (internal 0–1)

```
Final =
  Evidence*0.30 + Clinical*0.25 + Semantic*0.20 + Exact/Alias*0.15 + Keyword*0.10
```

Display: `MatchScore = round(canonical * 100)`.

## Rollback

`EnableFastClinicalRetrievalPipeline: false` → legacy V7/Enterprise path.

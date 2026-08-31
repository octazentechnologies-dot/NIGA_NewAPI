# Audio Case Intelligence V2 — SQL Scripts

**Manual deployment only.** Do not auto-apply from the application.

## Execution order

1. `001_Create_AudioCaseClinicalConcept.sql`
2. `002_Create_AudioCaseIntelligenceLog.sql`
3. `003_Create_RubricMetaphorDictionary.sql`
4. `004_Create_RubricAlias.sql`
5. `005_Create_RubricEmbeddings.sql`
6. `006_Create_HomeopathicWeightRule.sql`
7. `007_Create_AudioCaseCausationLink.sql`
8. `008_Create_AudioCaseClinicalInferenceLog.sql`
9. `009_Create_AudioCaseRubricFeedback.sql`
10. `010_Create_AudioCaseRubricBenchmark.sql`
11. `011_Create_RepertorySource.sql`
12. `012_Create_RubricRepertoryMap.sql`
13. `013_Create_RubricCrossReference.sql`
14. `014_Create_RubricAdminAuditLog.sql`
15. `015_Create_GoldCaseLibrary.sql`
16. `101_Alter_AudioCaseRubricMatchLog_V2Columns.sql`
17. `102_Alter_AudioCaseSession_V2Columns.sql`
18. `201_Indexes_All.sql`
19. `301_SP_SearchRubricAlias.sql`
20. `302_SP_SearchMetaphorDictionary.sql`
21. `501_Seed_MetaphorDictionary_EN_HI_MR.sql` (optional)
22. `502_Seed_RubricAlias_Batch001.sql` (optional)
23. `503_Seed_HomeopathicWeightRule.sql` (optional)
24. `504_Seed_RepertorySource_Kent_Complete.sql` (optional)

## Rollback

Run `401_Rollback_All_V2.sql` in reverse dependency order if needed.

## Phase 0 note

Phase 0 code runs without these tables. Deploy scripts before Phase 1+ features that persist intelligence data.

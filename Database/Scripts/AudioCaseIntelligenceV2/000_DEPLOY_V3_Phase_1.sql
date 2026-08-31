-- V3 Phase 1 deploy bundle (manual apply only)
-- Prerequisites: AudioCaseSession and V2 base tables deployed

:r 701_Create_AIConceptGraph_Core.sql
:r 702_Create_AIConceptGraph_Indexes.sql
:r 703_Seed_AIConceptGraph_Bootstrap.sql

PRINT '000_DEPLOY_V3_Phase_1 completed.';
GO

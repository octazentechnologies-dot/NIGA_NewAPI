-- Rollback Task 6 unified scoring columns

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'GroundedInOntology') IS NOT NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog DROP COLUMN GroundedInOntology;
GO
IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'EvidenceChainComplete') IS NOT NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog DROP COLUMN EvidenceChainComplete;
GO
IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'FinalHybridScore') IS NOT NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog DROP COLUMN FinalHybridScore;
GO
IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'UnifiedSource') IS NOT NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog DROP COLUMN UnifiedSource;
GO

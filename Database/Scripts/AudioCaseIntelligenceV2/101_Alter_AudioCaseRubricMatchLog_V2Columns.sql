-- Phase 5: Explainability columns on rubric match log

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'ClinicalMeaning') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD ClinicalMeaning NVARCHAR(1000) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'HomeopathicMeaning') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD HomeopathicMeaning NVARCHAR(1000) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'WhySuggested') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD WhySuggested NVARCHAR(2000) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'ConfidenceScore') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD ConfidenceScore DECIMAL(5,4) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'HomeopathicWeight') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD HomeopathicWeight DECIMAL(5,2) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'RubricTier') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD RubricTier NVARCHAR(30) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'MatchLayer') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD MatchLayer NVARCHAR(50) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'CausationJson') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD CausationJson NVARCHAR(MAX) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'ExplainabilityJson') IS NULL
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD ExplainabilityJson NVARCHAR(MAX) NULL;
GO

PRINT 'AudioCaseRubricMatchLog V2 explainability columns added.';
GO

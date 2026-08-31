-- Phase 1: Session columns for clinical concepts JSON cache

IF COL_LENGTH('dbo.AudioCaseSession', 'ClinicalConceptsJson') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession
        ADD ClinicalConceptsJson NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('dbo.AudioCaseSession', 'IntelligenceEngineVersion') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession
        ADD IntelligenceEngineVersion NVARCHAR(10) NULL;
END
GO

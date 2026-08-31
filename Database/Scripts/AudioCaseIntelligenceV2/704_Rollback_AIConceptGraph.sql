-- V3 rollback: drop Concept Graph tables only (does not touch V2 or master tables)

IF OBJECT_ID(N'dbo.AIDoctorFeedback', N'U') IS NOT NULL DROP TABLE dbo.AIDoctorFeedback;
GO
IF OBJECT_ID(N'dbo.AICaseLearning', N'U') IS NOT NULL DROP TABLE dbo.AICaseLearning;
GO
IF OBJECT_ID(N'dbo.AIRubricConfidence', N'U') IS NOT NULL DROP TABLE dbo.AIRubricConfidence;
GO
IF OBJECT_ID(N'dbo.AIRubricValidation', N'U') IS NOT NULL DROP TABLE dbo.AIRubricValidation;
GO
IF OBJECT_ID(N'dbo.AIRubricEvidence', N'U') IS NOT NULL DROP TABLE dbo.AIRubricEvidence;
GO
IF OBJECT_ID(N'dbo.AIRubricDiscovery', N'U') IS NOT NULL DROP TABLE dbo.AIRubricDiscovery;
GO
IF OBJECT_ID(N'dbo.AIConceptGraph', N'U') IS NOT NULL DROP TABLE dbo.AIConceptGraph;
GO
IF OBJECT_ID(N'dbo.AIHomeopathicConcept', N'U') IS NOT NULL DROP TABLE dbo.AIHomeopathicConcept;
GO
IF OBJECT_ID(N'dbo.AIClinicalConcept', N'U') IS NOT NULL DROP TABLE dbo.AIClinicalConcept;
GO
IF OBJECT_ID(N'dbo.AIMetaphorResolution', N'U') IS NOT NULL DROP TABLE dbo.AIMetaphorResolution;
GO
IF OBJECT_ID(N'dbo.AIPatientMeaning', N'U') IS NOT NULL DROP TABLE dbo.AIPatientMeaning;
GO
IF OBJECT_ID(N'dbo.AIReasoningAudit', N'U') IS NOT NULL DROP TABLE dbo.AIReasoningAudit;
GO
IF OBJECT_ID(N'dbo.AIConceptMappingBootstrap', N'U') IS NOT NULL DROP TABLE dbo.AIConceptMappingBootstrap;
GO

IF COL_LENGTH('dbo.AudioCaseSession', 'ConceptGraphEngineVersion') IS NOT NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession DROP COLUMN ConceptGraphEngineVersion;
END
GO

PRINT '704_Rollback_AIConceptGraph completed.';
GO

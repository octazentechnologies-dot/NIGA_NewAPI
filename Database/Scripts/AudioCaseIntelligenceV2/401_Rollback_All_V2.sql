-- Rollback all V2 intelligence objects (run manually when needed)
-- Review FK order before executing in production

IF OBJECT_ID('dbo.FK_AudioCaseClinicalConcept_Session', 'F') IS NOT NULL
    ALTER TABLE dbo.AudioCaseClinicalConcept DROP CONSTRAINT FK_AudioCaseClinicalConcept_Session;
GO
IF OBJECT_ID('dbo.AudioCaseClinicalConcept', 'U') IS NOT NULL DROP TABLE dbo.AudioCaseClinicalConcept;
GO

IF OBJECT_ID('dbo.FK_AudioCaseIntelligenceLog_Session', 'F') IS NOT NULL
    ALTER TABLE dbo.AudioCaseIntelligenceLog DROP CONSTRAINT FK_AudioCaseIntelligenceLog_Session;
GO
IF OBJECT_ID('dbo.AudioCaseIntelligenceLog', 'U') IS NOT NULL DROP TABLE dbo.AudioCaseIntelligenceLog;
GO

IF OBJECT_ID('dbo.RubricMetaphorDictionary', 'U') IS NOT NULL DROP TABLE dbo.RubricMetaphorDictionary;
GO

IF OBJECT_ID('dbo.FK_RubricAlias_SubSection', 'F') IS NOT NULL
    ALTER TABLE dbo.RubricAlias DROP CONSTRAINT FK_RubricAlias_SubSection;
GO
IF OBJECT_ID('dbo.RubricAlias', 'U') IS NOT NULL DROP TABLE dbo.RubricAlias;
GO

IF OBJECT_ID('dbo.FK_RubricEmbeddings_SubSection', 'F') IS NOT NULL
    ALTER TABLE dbo.RubricEmbeddings DROP CONSTRAINT FK_RubricEmbeddings_SubSection;
GO
IF OBJECT_ID('dbo.RubricEmbeddings', 'U') IS NOT NULL DROP TABLE dbo.RubricEmbeddings;
GO

IF OBJECT_ID('dbo.RubricAdminAuditLog', 'U') IS NOT NULL DROP TABLE dbo.RubricAdminAuditLog;
GO

IF OBJECT_ID('dbo.GoldCaseLibrary', 'U') IS NOT NULL DROP TABLE dbo.GoldCaseLibrary;
GO

IF OBJECT_ID('dbo.FK_RubricRepertoryMap_SubSection', 'F') IS NOT NULL
    ALTER TABLE dbo.RubricRepertoryMap DROP CONSTRAINT FK_RubricRepertoryMap_SubSection;
GO
IF OBJECT_ID('dbo.FK_RubricRepertoryMap_Source', 'F') IS NOT NULL
    ALTER TABLE dbo.RubricRepertoryMap DROP CONSTRAINT FK_RubricRepertoryMap_Source;
GO
IF OBJECT_ID('dbo.RubricRepertoryMap', 'U') IS NOT NULL DROP TABLE dbo.RubricRepertoryMap;
GO

IF OBJECT_ID('dbo.RepertorySource', 'U') IS NOT NULL DROP TABLE dbo.RepertorySource;
GO

IF COL_LENGTH('dbo.AudioCaseSession', 'ClinicalConceptsJson') IS NOT NULL
    ALTER TABLE dbo.AudioCaseSession DROP COLUMN ClinicalConceptsJson;
GO
IF COL_LENGTH('dbo.AudioCaseSession', 'IntelligenceEngineVersion') IS NOT NULL
    ALTER TABLE dbo.AudioCaseSession DROP COLUMN IntelligenceEngineVersion;
GO
IF COL_LENGTH('dbo.AudioCaseSession', 'CausationLinksJson') IS NOT NULL
    ALTER TABLE dbo.AudioCaseSession DROP COLUMN CausationLinksJson;
GO

IF OBJECT_ID('dbo.FK_AudioCaseCausationLink_Session', 'F') IS NOT NULL
    ALTER TABLE dbo.AudioCaseCausationLink DROP CONSTRAINT FK_AudioCaseCausationLink_Session;
GO
IF OBJECT_ID('dbo.AudioCaseCausationLink', 'U') IS NOT NULL DROP TABLE dbo.AudioCaseCausationLink;
GO

IF OBJECT_ID('dbo.HomeopathicWeightRule', 'U') IS NOT NULL DROP TABLE dbo.HomeopathicWeightRule;
GO

IF OBJECT_ID('dbo.FK_AudioCaseClinicalInferenceLog_Session', 'F') IS NOT NULL
    ALTER TABLE dbo.AudioCaseClinicalInferenceLog DROP CONSTRAINT FK_AudioCaseClinicalInferenceLog_Session;
GO
IF OBJECT_ID('dbo.AudioCaseClinicalInferenceLog', 'U') IS NOT NULL DROP TABLE dbo.AudioCaseClinicalInferenceLog;
GO

IF OBJECT_ID('dbo.FK_AudioCaseRubricFeedback_Session', 'F') IS NOT NULL
    ALTER TABLE dbo.AudioCaseRubricFeedback DROP CONSTRAINT FK_AudioCaseRubricFeedback_Session;
GO
IF OBJECT_ID('dbo.AudioCaseRubricFeedback', 'U') IS NOT NULL DROP TABLE dbo.AudioCaseRubricFeedback;
GO

IF OBJECT_ID('dbo.FK_AudioCaseRubricBenchmark_Session', 'F') IS NOT NULL
    ALTER TABLE dbo.AudioCaseRubricBenchmark DROP CONSTRAINT FK_AudioCaseRubricBenchmark_Session;
GO
IF OBJECT_ID('dbo.AudioCaseRubricBenchmark', 'U') IS NOT NULL DROP TABLE dbo.AudioCaseRubricBenchmark;
GO

PRINT 'V2 intelligence rollback complete (partial — extend as more scripts are deployed).';
GO

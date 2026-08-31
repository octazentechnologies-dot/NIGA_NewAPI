-- Phase 4: Additional indexes for embedding + intelligence queries
-- Run AFTER 005_Create_RubricEmbeddings.sql

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RubricEmbeddings_ModelName_RubricId'
      AND object_id = OBJECT_ID('dbo.RubricEmbeddings'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricEmbeddings_ModelName_RubricId
        ON dbo.RubricEmbeddings (ModelName, RubricId)
        INCLUDE (TextHash, UpdatedDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RubricEmbeddings_SourceType'
      AND object_id = OBJECT_ID('dbo.RubricEmbeddings'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricEmbeddings_SourceType
        ON dbo.RubricEmbeddings (SourceType, RubricId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioCaseClinicalConcept_SessionId'
      AND object_id = OBJECT_ID('dbo.AudioCaseClinicalConcept'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AudioCaseClinicalConcept_SessionId
        ON dbo.AudioCaseClinicalConcept (AudioCaseSessionId)
        INCLUDE (Category, Confidence, EnteredDate);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioCaseIntelligenceLog_SessionStage'
      AND object_id = OBJECT_ID('dbo.AudioCaseIntelligenceLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AudioCaseIntelligenceLog_SessionStage
        ON dbo.AudioCaseIntelligenceLog (AudioCaseSessionId, StageName, EnteredDate DESC);
END
GO

PRINT 'Phase 4 performance indexes applied.';
GO

-- V3 Phase 11: Concept Graph indexes

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIPatientMeaning_Session' AND object_id = OBJECT_ID(N'dbo.AIPatientMeaning'))
    CREATE INDEX IX_AIPatientMeaning_Session ON dbo.AIPatientMeaning (AudioCaseSessionId, SequenceOrder);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIMetaphorResolution_Session' AND object_id = OBJECT_ID(N'dbo.AIMetaphorResolution'))
    CREATE INDEX IX_AIMetaphorResolution_Session ON dbo.AIMetaphorResolution (AudioCaseSessionId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIClinicalConcept_Session' AND object_id = OBJECT_ID(N'dbo.AIClinicalConcept'))
    CREATE INDEX IX_AIClinicalConcept_Session ON dbo.AIClinicalConcept (AudioCaseSessionId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIHomeopathicConcept_Session' AND object_id = OBJECT_ID(N'dbo.AIHomeopathicConcept'))
    CREATE INDEX IX_AIHomeopathicConcept_Session ON dbo.AIHomeopathicConcept (AudioCaseSessionId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIConceptGraph_Session' AND object_id = OBJECT_ID(N'dbo.AIConceptGraph'))
    CREATE INDEX IX_AIConceptGraph_Session ON dbo.AIConceptGraph (AudioCaseSessionId, EdgeType);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIRubricDiscovery_Session' AND object_id = OBJECT_ID(N'dbo.AIRubricDiscovery'))
    CREATE INDEX IX_AIRubricDiscovery_Session ON dbo.AIRubricDiscovery (AudioCaseSessionId, SubSectionId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIReasoningAudit_Session' AND object_id = OBJECT_ID(N'dbo.AIReasoningAudit'))
    CREATE INDEX IX_AIReasoningAudit_Session ON dbo.AIReasoningAudit (AudioCaseSessionId, PipelineStage, EnteredDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIDoctorFeedback_Session' AND object_id = OBJECT_ID(N'dbo.AIDoctorFeedback'))
    CREATE INDEX IX_AIDoctorFeedback_Session ON dbo.AIDoctorFeedback (AudioCaseSessionId, EnteredDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AICaseLearning_Concept' AND object_id = OBJECT_ID(N'dbo.AICaseLearning'))
    CREATE INDEX IX_AICaseLearning_Concept ON dbo.AICaseLearning (FromConcept, LearningType);
GO

PRINT '702_Create_AIConceptGraph_Indexes completed.';
GO

-- =============================================================================
-- HomeoCentrum AI Rubric Intelligence V3 — COMPLETE DEPLOY SCRIPT (ALL PHASES)
-- Includes: V3 Concept Graph (Phases 1-9 tables) + V2.1 Clinical Validation
-- Manual execution only. Does NOT modify SectionMaster, SubSectionMaster,
-- or any existing repertory master tables.
-- Prerequisites: AudioCaseSession table must exist (Audio Case Taking module).
-- Rollback: 704_Rollback_AIConceptGraph.sql (V3 tables only)
-- =============================================================================

SET NOCOUNT ON;
PRINT '=== V3 DEPLOY START ===';
GO

/* ---------- CORE TABLES ---------- */

IF OBJECT_ID(N'dbo.AIPatientMeaning', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIPatientMeaning
    (
        PatientMeaningId     BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        RawStatement         NVARCHAR(2000) NOT NULL,
        NormalizedMeaning    NVARCHAR(1000) NOT NULL,
        LanguageCode         NVARCHAR(10) NOT NULL CONSTRAINT DF_AIPatientMeaning_Language DEFAULT (N'en'),
        Confidence           DECIMAL(5,4) NOT NULL,
        SequenceOrder        INT NOT NULL CONSTRAINT DF_AIPatientMeaning_Sequence DEFAULT (0),
        ModelVersion         NVARCHAR(20) NOT NULL CONSTRAINT DF_AIPatientMeaning_Model DEFAULT (N'v3-m1'),
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIPatientMeaning_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIPatientMeaning PRIMARY KEY (PatientMeaningId),
        CONSTRAINT FK_AIPatientMeaning_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIPatientMeaning';
END
GO

IF OBJECT_ID(N'dbo.AIMetaphorResolution', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIMetaphorResolution
    (
        MetaphorResolutionId BIGINT IDENTITY(1,1) NOT NULL,
        PatientMeaningId     BIGINT NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        Expression           NVARCHAR(1000) NOT NULL,
        LiteralMeaning       NVARCHAR(1000) NULL,
        ClinicalMeaning      NVARCHAR(1000) NOT NULL,
        Confidence           DECIMAL(5,4) NOT NULL,
        Source               NVARCHAR(20) NOT NULL CONSTRAINT DF_AIMetaphorResolution_Source DEFAULT (N'ai'),
        ModelVersion         NVARCHAR(20) NOT NULL CONSTRAINT DF_AIMetaphorResolution_Model DEFAULT (N'v3-m2'),
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIMetaphorResolution_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIMetaphorResolution PRIMARY KEY (MetaphorResolutionId),
        CONSTRAINT FK_AIMetaphorResolution_Meaning FOREIGN KEY (PatientMeaningId) REFERENCES dbo.AIPatientMeaning (PatientMeaningId),
        CONSTRAINT FK_AIMetaphorResolution_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIMetaphorResolution';
END
GO

IF OBJECT_ID(N'dbo.AIClinicalConcept', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIClinicalConcept
    (
        ClinicalConceptId    BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        PatientMeaningId     BIGINT NULL,
        ConceptName          NVARCHAR(500) NOT NULL,
        Domain               NVARCHAR(100) NOT NULL,
        Confidence           DECIMAL(5,4) NOT NULL,
        ModelVersion         NVARCHAR(20) NOT NULL CONSTRAINT DF_AIClinicalConcept_Model DEFAULT (N'v3-m3'),
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIClinicalConcept_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIClinicalConcept PRIMARY KEY (ClinicalConceptId),
        CONSTRAINT FK_AIClinicalConcept_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId),
        CONSTRAINT FK_AIClinicalConcept_Meaning FOREIGN KEY (PatientMeaningId) REFERENCES dbo.AIPatientMeaning (PatientMeaningId)
    );
    PRINT 'Created AIClinicalConcept';
END
GO

IF OBJECT_ID(N'dbo.AIHomeopathicConcept', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIHomeopathicConcept
    (
        HomeopathicConceptId BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        ClinicalConceptId    BIGINT NOT NULL,
        ConceptName          NVARCHAR(500) NOT NULL,
        Importance           NVARCHAR(20) NOT NULL,
        SymptomClass         NVARCHAR(50) NULL,
        IsSRP                BIT NOT NULL CONSTRAINT DF_AIHomeopathicConcept_IsSRP DEFAULT (0),
        Weight               DECIMAL(6,3) NOT NULL CONSTRAINT DF_AIHomeopathicConcept_Weight DEFAULT (1.000),
        Confidence           DECIMAL(5,4) NOT NULL,
        ModelVersion         NVARCHAR(20) NOT NULL CONSTRAINT DF_AIHomeopathicConcept_Model DEFAULT (N'v3-m4'),
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIHomeopathicConcept_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIHomeopathicConcept PRIMARY KEY (HomeopathicConceptId),
        CONSTRAINT FK_AIHomeopathicConcept_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId),
        CONSTRAINT FK_AIHomeopathicConcept_Clinical FOREIGN KEY (ClinicalConceptId) REFERENCES dbo.AIClinicalConcept (ClinicalConceptId)
    );
    PRINT 'Created AIHomeopathicConcept';
END
GO

IF OBJECT_ID(N'dbo.AIConceptGraph', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIConceptGraph
    (
        ConceptGraphEdgeId   BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        FromNodeType         NVARCHAR(50) NOT NULL,
        FromNodeId           BIGINT NOT NULL,
        ToNodeType           NVARCHAR(50) NOT NULL,
        ToNodeId             BIGINT NOT NULL,
        EdgeType             NVARCHAR(50) NOT NULL,
        Weight               DECIMAL(6,3) NOT NULL CONSTRAINT DF_AIConceptGraph_Weight DEFAULT (1.000),
        Confidence           DECIMAL(5,4) NOT NULL,
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIConceptGraph_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIConceptGraph PRIMARY KEY (ConceptGraphEdgeId),
        CONSTRAINT FK_AIConceptGraph_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIConceptGraph';
END
GO

IF OBJECT_ID(N'dbo.AIRubricDiscovery', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIRubricDiscovery
    (
        RubricDiscoveryId    BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        HomeopathicConceptId BIGINT NOT NULL,
        SubSectionId         INT NOT NULL,
        SubSectionName       NVARCHAR(500) NOT NULL,
        MatchReason          NVARCHAR(1000) NULL,
        DiscoveryMethod      NVARCHAR(50) NOT NULL,
        Confidence           DECIMAL(5,4) NOT NULL,
        ModelVersion         NVARCHAR(20) NOT NULL CONSTRAINT DF_AIRubricDiscovery_Model DEFAULT (N'v3-m5'),
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIRubricDiscovery_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIRubricDiscovery PRIMARY KEY (RubricDiscoveryId),
        CONSTRAINT FK_AIRubricDiscovery_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId),
        CONSTRAINT FK_AIRubricDiscovery_Homeopathic FOREIGN KEY (HomeopathicConceptId) REFERENCES dbo.AIHomeopathicConcept (HomeopathicConceptId)
    );
    PRINT 'Created AIRubricDiscovery';
END
GO

IF OBJECT_ID(N'dbo.AIRubricEvidence', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIRubricEvidence
    (
        RubricEvidenceId     BIGINT IDENTITY(1,1) NOT NULL,
        RubricDiscoveryId    BIGINT NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        EvidenceChainJson    NVARCHAR(MAX) NOT NULL,
        IsComplete           BIT NOT NULL CONSTRAINT DF_AIRubricEvidence_IsComplete DEFAULT (0),
        CoverageScore        DECIMAL(5,4) NOT NULL,
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIRubricEvidence_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIRubricEvidence PRIMARY KEY (RubricEvidenceId),
        CONSTRAINT FK_AIRubricEvidence_Discovery FOREIGN KEY (RubricDiscoveryId) REFERENCES dbo.AIRubricDiscovery (RubricDiscoveryId),
        CONSTRAINT FK_AIRubricEvidence_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIRubricEvidence';
END
GO

IF OBJECT_ID(N'dbo.AIRubricValidation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIRubricValidation
    (
        RubricValidationId   BIGINT IDENTITY(1,1) NOT NULL,
        RubricDiscoveryId    BIGINT NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        ValidationStatus     NVARCHAR(20) NOT NULL,
        QualityScore         DECIMAL(5,2) NULL,
        ValidationFlagsJson  NVARCHAR(2000) NULL,
        ValidatedAt          DATETIME2 NOT NULL CONSTRAINT DF_AIRubricValidation_ValidatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIRubricValidation PRIMARY KEY (RubricValidationId),
        CONSTRAINT FK_AIRubricValidation_Discovery FOREIGN KEY (RubricDiscoveryId) REFERENCES dbo.AIRubricDiscovery (RubricDiscoveryId),
        CONSTRAINT FK_AIRubricValidation_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIRubricValidation';
END
GO

IF OBJECT_ID(N'dbo.AIRubricConfidence', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIRubricConfidence
    (
        RubricConfidenceId   BIGINT IDENTITY(1,1) NOT NULL,
        RubricDiscoveryId    BIGINT NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        FinalScore           DECIMAL(5,4) NOT NULL,
        RankOrder            INT NOT NULL,
        Tier                 NVARCHAR(20) NULL,
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIRubricConfidence_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIRubricConfidence PRIMARY KEY (RubricConfidenceId),
        CONSTRAINT FK_AIRubricConfidence_Discovery FOREIGN KEY (RubricDiscoveryId) REFERENCES dbo.AIRubricDiscovery (RubricDiscoveryId),
        CONSTRAINT FK_AIRubricConfidence_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIRubricConfidence';
END
GO

IF OBJECT_ID(N'dbo.AIDoctorFeedback', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIDoctorFeedback
    (
        DoctorFeedbackId     BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        RubricDiscoveryId    BIGINT NULL,
        Action               NVARCHAR(20) NOT NULL,
        Reason               NVARCHAR(1000) NULL,
        DoctorUserId         INT NOT NULL,
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIDoctorFeedback_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIDoctorFeedback PRIMARY KEY (DoctorFeedbackId),
        CONSTRAINT FK_AIDoctorFeedback_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId),
        CONSTRAINT FK_AIDoctorFeedback_Discovery FOREIGN KEY (RubricDiscoveryId) REFERENCES dbo.AIRubricDiscovery (RubricDiscoveryId)
    );
    PRINT 'Created AIDoctorFeedback';
END
GO

IF OBJECT_ID(N'dbo.AICaseLearning', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AICaseLearning
    (
        CaseLearningId       BIGINT IDENTITY(1,1) NOT NULL,
        SourceSessionId      UNIQUEIDENTIFIER NOT NULL,
        LearningType         NVARCHAR(50) NOT NULL,
        FromConcept          NVARCHAR(500) NOT NULL,
        ToRubricSubSectionId INT NULL,
        WeightDelta          DECIMAL(6,3) NOT NULL,
        ContextJson          NVARCHAR(MAX) NULL,
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AICaseLearning_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AICaseLearning PRIMARY KEY (CaseLearningId),
        CONSTRAINT FK_AICaseLearning_Session FOREIGN KEY (SourceSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AICaseLearning';
END
GO

IF OBJECT_ID(N'dbo.AIReasoningAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIReasoningAudit
    (
        ReasoningAuditId     BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        PipelineStage        NVARCHAR(50) NOT NULL,
        ModelId              NVARCHAR(20) NOT NULL,
        RequestJson          NVARCHAR(MAX) NULL,
        ResponseJson         NVARCHAR(MAX) NULL,
        LatencyMs            INT NULL,
        Success              BIT NOT NULL,
        ErrorMessage         NVARCHAR(2000) NULL,
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AIReasoningAudit_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIReasoningAudit PRIMARY KEY (ReasoningAuditId),
        CONSTRAINT FK_AIReasoningAudit_Session FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIReasoningAudit';
END
GO

IF OBJECT_ID(N'dbo.AIConceptMappingBootstrap', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIConceptMappingBootstrap
    (
        ConceptMappingBootstrapId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HomeopathicConceptPattern NVARCHAR(500) NOT NULL,
        SubSectionNamePattern     NVARCHAR(500) NOT NULL,
        Domain                    NVARCHAR(100) NULL,
        PriorityOrder             INT NOT NULL CONSTRAINT DF_AIConceptMappingBootstrap_Priority DEFAULT (1),
        IsActive                  BIT NOT NULL CONSTRAINT DF_AIConceptMappingBootstrap_Active DEFAULT (1),
        EnteredDate               DATETIME2 NOT NULL CONSTRAINT DF_AIConceptMappingBootstrap_Entered DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created AIConceptMappingBootstrap';
END
GO

IF COL_LENGTH('dbo.AudioCaseSession', 'ConceptGraphEngineVersion') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession ADD ConceptGraphEngineVersion NVARCHAR(10) NULL;
    PRINT 'Added AudioCaseSession.ConceptGraphEngineVersion';
END
GO

/* ---------- INDEXES ---------- */

IF OBJECT_ID(N'dbo.AIPatientMeaning', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIPatientMeaning_Session' AND object_id = OBJECT_ID(N'dbo.AIPatientMeaning'))
    CREATE INDEX IX_AIPatientMeaning_Session ON dbo.AIPatientMeaning (AudioCaseSessionId, SequenceOrder);
GO

IF OBJECT_ID(N'dbo.AIMetaphorResolution', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIMetaphorResolution_Session' AND object_id = OBJECT_ID(N'dbo.AIMetaphorResolution'))
    CREATE INDEX IX_AIMetaphorResolution_Session ON dbo.AIMetaphorResolution (AudioCaseSessionId);
GO

IF OBJECT_ID(N'dbo.AIClinicalConcept', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIClinicalConcept_Session' AND object_id = OBJECT_ID(N'dbo.AIClinicalConcept'))
    CREATE INDEX IX_AIClinicalConcept_Session ON dbo.AIClinicalConcept (AudioCaseSessionId);
GO

IF OBJECT_ID(N'dbo.AIHomeopathicConcept', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIHomeopathicConcept_Session' AND object_id = OBJECT_ID(N'dbo.AIHomeopathicConcept'))
    CREATE INDEX IX_AIHomeopathicConcept_Session ON dbo.AIHomeopathicConcept (AudioCaseSessionId);
GO

IF OBJECT_ID(N'dbo.AIConceptGraph', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIConceptGraph_Session' AND object_id = OBJECT_ID(N'dbo.AIConceptGraph'))
    CREATE INDEX IX_AIConceptGraph_Session ON dbo.AIConceptGraph (AudioCaseSessionId, EdgeType);
GO

IF OBJECT_ID(N'dbo.AIRubricDiscovery', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIRubricDiscovery_Session' AND object_id = OBJECT_ID(N'dbo.AIRubricDiscovery'))
    CREATE INDEX IX_AIRubricDiscovery_Session ON dbo.AIRubricDiscovery (AudioCaseSessionId, SubSectionId);
GO

IF OBJECT_ID(N'dbo.AIReasoningAudit', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIReasoningAudit_Session' AND object_id = OBJECT_ID(N'dbo.AIReasoningAudit'))
    CREATE INDEX IX_AIReasoningAudit_Session ON dbo.AIReasoningAudit (AudioCaseSessionId, PipelineStage, EnteredDate DESC);
GO

IF OBJECT_ID(N'dbo.AIDoctorFeedback', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIDoctorFeedback_Session' AND object_id = OBJECT_ID(N'dbo.AIDoctorFeedback'))
    CREATE INDEX IX_AIDoctorFeedback_Session ON dbo.AIDoctorFeedback (AudioCaseSessionId, EnteredDate DESC);
GO

IF OBJECT_ID(N'dbo.AICaseLearning', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AICaseLearning_Concept' AND object_id = OBJECT_ID(N'dbo.AICaseLearning'))
    CREATE INDEX IX_AICaseLearning_Concept ON dbo.AICaseLearning (FromConcept, LearningType);
GO

IF OBJECT_ID(N'dbo.AIConceptMappingBootstrap', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIConceptMappingBootstrap_Pattern' AND object_id = OBJECT_ID(N'dbo.AIConceptMappingBootstrap'))
    CREATE INDEX IX_AIConceptMappingBootstrap_Pattern ON dbo.AIConceptMappingBootstrap (HomeopathicConceptPattern, IsActive);
GO

/* ---------- SEED DATA ---------- */

IF OBJECT_ID(N'dbo.AIConceptMappingBootstrap', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.AIConceptMappingBootstrap WHERE HomeopathicConceptPattern = N'Convulsion Aura')
BEGIN
    INSERT INTO dbo.AIConceptMappingBootstrap (HomeopathicConceptPattern, SubSectionNamePattern, Domain, PriorityOrder) VALUES
    (N'Convulsion Aura',                    N'%CONVULS%AURA%',              N'Neurology', 1),
    (N'Convulsion Aura',                    N'%AURA%CONVULS%',              N'Neurology', 2),
    (N'Fear Before Convulsion',             N'%FEAR%CONVULS%',             N'Neurology', 1),
    (N'Fear Before Convulsion',             N'%FEAR%FIT%',                 N'Neurology', 2),
    (N'Fear Before Convulsion',             N'%FEAR%EPILEP%',              N'Neurology', 3),
    (N'Anticipatory Fear of Seizure',       N'%FEAR%CONVULS%',             N'Neurology', 1),
    (N'Anticipatory Fear of Seizure',       N'%FEAR%FIT%',                 N'Neurology', 2),
    (N'Motor Weakness Before Convulsion',   N'%DROPPING%THING%',           N'Neurology', 1),
    (N'Motor Weakness Before Convulsion',   N'%DROP%HAND%',                N'Neurology', 2),
    (N'Loss of Grip',                       N'%DROPPING%THING%',           N'Neurology', 1),
    (N'Prodromal Shivering',                N'%SHIVER%CONVULS%',           N'Neurology', 1),
    (N'Prodromal Shivering',                N'%TREMOR%BEFORE%',             N'Neurology', 2),
    (N'Epilepsy',                           N'%EPILEP%',                   N'Neurology', 1),
    (N'Epilepsy',                           N'%CONVULS%',                  N'Neurology', 2),
    (N'Electric Shock Sensation',           N'%ELECTRIC%SHOCK%',           N'General',   1),
    (N'Electric Shock Sensation',           N'%CURRENT%BODY%',             N'General',   2),
    (N'Palpitation',                        N'%PALPITAT%',                 N'Cardiac',   1),
    (N'Bursting Headache',                  N'%BURST%HEAD%',               N'Head',      1),
    (N'Chest Constriction',                 N'%CONSTRICT%CHEST%',          N'Chest',     1),
    (N'Chest Constriction',                 N'%CHEST%STONE%',              N'Chest',     2);
    PRINT 'Seeded AIConceptMappingBootstrap (20 patterns)';
END
GO

PRINT '=== V3 DEPLOY COMPLETE ===';
GO

/* =============================================================================
   V2.1 CLINICAL VALIDATION (required for V3 Phase 6 validation layer)
   ============================================================================= */

IF OBJECT_ID(N'dbo.RubricGenderRule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RubricGenderRule
    (
        RubricGenderRuleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TokenPattern NVARCHAR(100) NOT NULL,
        AllowedGender TINYINT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_RubricGenderRule_IsActive DEFAULT (1),
        EnteredDate DATETIME2 NOT NULL CONSTRAINT DF_RubricGenderRule_EnteredDate DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created RubricGenderRule';
END
GO

IF OBJECT_ID(N'dbo.RubricDomainRule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RubricDomainRule
    (
        RubricDomainRuleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SectionPrefix NVARCHAR(50) NOT NULL,
        KeywordHints NVARCHAR(1000) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_RubricDomainRule_IsActive DEFAULT (1),
        EnteredDate DATETIME2 NOT NULL CONSTRAINT DF_RubricDomainRule_EnteredDate DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created RubricDomainRule';
END
GO

IF OBJECT_ID(N'dbo.AudioCaseRubricValidationLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AudioCaseRubricValidationLog
    (
        AudioCaseRubricValidationLogId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        SubSectionId INT NULL,
        SubSectionName NVARCHAR(500) NOT NULL,
        ValidationStatus NVARCHAR(20) NOT NULL,
        QualityScore DECIMAL(5,2) NULL,
        ValidationFlagsJson NVARCHAR(2000) NULL,
        EvidenceChainJson NVARCHAR(MAX) NULL,
        EnteredDate DATETIME2 NOT NULL CONSTRAINT DF_AudioCaseRubricValidationLog_EnteredDate DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_AudioCaseRubricValidationLog_Session
        ON dbo.AudioCaseRubricValidationLog (AudioCaseSessionId, EnteredDate DESC);
    PRINT 'Created AudioCaseRubricValidationLog';
END
GO

IF OBJECT_ID(N'dbo.PrimarySymptomLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PrimarySymptomLog
    (
        PrimarySymptomLogId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        PrimarySymptomText NVARCHAR(1000) NOT NULL,
        Source NVARCHAR(50) NOT NULL,
        SourceConceptId UNIQUEIDENTIFIER NULL,
        Confidence DECIMAL(5,4) NULL,
        EnteredDate DATETIME2 NOT NULL CONSTRAINT DF_PrimarySymptomLog_EnteredDate DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created PrimarySymptomLog';
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'EvidenceChainJson') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD EvidenceChainJson NVARCHAR(MAX) NULL;
    PRINT 'Added AudioCaseRubricMatchLog.EvidenceChainJson';
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'QualityScore') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD QualityScore DECIMAL(5,2) NULL;
    PRINT 'Added AudioCaseRubricMatchLog.QualityScore';
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'ValidationStatus') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD ValidationStatus NVARCHAR(20) NULL;
    PRINT 'Added AudioCaseRubricMatchLog.ValidationStatus';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RubricGenderRule WHERE TokenPattern = N'MENSES')
BEGIN
    INSERT INTO dbo.RubricGenderRule (TokenPattern, AllowedGender) VALUES
    (N'MENSES', 1), (N'MENSTRU', 1), (N'PREGNAN', 1), (N'LABOR', 1), (N'OVAR', 1),
    (N'UTER', 1), (N'VAGIN', 1), (N'LOCHIA', 1), (N'MISCARRI', 1), (N'MENOPAUSE', 1),
    (N'PROSTATE', 0), (N'TESTES', 0), (N'TESTIC', 0), (N'SCROTUM', 0);
    PRINT 'Seeded RubricGenderRule (14 tokens)';
END
GO

PRINT '=== V3 + V2.1 VALIDATION DEPLOY COMPLETE ===';
PRINT 'Next steps:';
PRINT '  1. Deploy updated API';
PRINT '  2. Set EnableV3ConceptGraph=true in appsettings.json';
PRINT '  3. Keep EnableV3ShadowMode=true for shadow testing first';
PRINT '  4. Set EnableV3ShadowMode=false when ready for V3 rubrics in UI';
GO

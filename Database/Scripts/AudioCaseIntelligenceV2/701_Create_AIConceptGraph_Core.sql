-- V3 Phase 11: Concept Graph core tables (new tables only — manual deploy)
-- Does NOT modify SectionMaster, SubSectionMaster, or existing repertory tables.

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
        CONSTRAINT FK_AIMetaphorResolution_Meaning FOREIGN KEY (PatientMeaningId)
            REFERENCES dbo.AIPatientMeaning (PatientMeaningId),
        CONSTRAINT FK_AIMetaphorResolution_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
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
        CONSTRAINT FK_AIClinicalConcept_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId),
        CONSTRAINT FK_AIClinicalConcept_Meaning FOREIGN KEY (PatientMeaningId)
            REFERENCES dbo.AIPatientMeaning (PatientMeaningId)
    );
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
        CONSTRAINT FK_AIHomeopathicConcept_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId),
        CONSTRAINT FK_AIHomeopathicConcept_Clinical FOREIGN KEY (ClinicalConceptId)
            REFERENCES dbo.AIClinicalConcept (ClinicalConceptId)
    );
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
        CONSTRAINT FK_AIConceptGraph_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
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
        CONSTRAINT FK_AIRubricDiscovery_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId),
        CONSTRAINT FK_AIRubricDiscovery_Homeopathic FOREIGN KEY (HomeopathicConceptId)
            REFERENCES dbo.AIHomeopathicConcept (HomeopathicConceptId)
    );
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
        CONSTRAINT FK_AIRubricEvidence_Discovery FOREIGN KEY (RubricDiscoveryId)
            REFERENCES dbo.AIRubricDiscovery (RubricDiscoveryId),
        CONSTRAINT FK_AIRubricEvidence_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
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
        CONSTRAINT FK_AIRubricValidation_Discovery FOREIGN KEY (RubricDiscoveryId)
            REFERENCES dbo.AIRubricDiscovery (RubricDiscoveryId),
        CONSTRAINT FK_AIRubricValidation_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
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
        CONSTRAINT FK_AIRubricConfidence_Discovery FOREIGN KEY (RubricDiscoveryId)
            REFERENCES dbo.AIRubricDiscovery (RubricDiscoveryId),
        CONSTRAINT FK_AIRubricConfidence_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
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
        CONSTRAINT FK_AIDoctorFeedback_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId),
        CONSTRAINT FK_AIDoctorFeedback_Discovery FOREIGN KEY (RubricDiscoveryId)
            REFERENCES dbo.AIRubricDiscovery (RubricDiscoveryId)
    );
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
        CONSTRAINT FK_AICaseLearning_Session FOREIGN KEY (SourceSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
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
        CONSTRAINT FK_AIReasoningAudit_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO

IF COL_LENGTH('dbo.AudioCaseSession', 'ConceptGraphEngineVersion') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession ADD ConceptGraphEngineVersion NVARCHAR(10) NULL;
END
GO

PRINT '701_Create_AIConceptGraph_Core completed.';
GO

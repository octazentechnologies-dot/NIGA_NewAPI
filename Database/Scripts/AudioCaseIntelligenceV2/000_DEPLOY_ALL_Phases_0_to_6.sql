/*
    HomeoCentrum Audio Case Taking AI Engine V2
    CONSOLIDATED DEPLOYMENT — Phases 0 through 6 (21 scripts)
    Database: HomeoCentrum_Production
    Run manually in SSMS. Take a full backup first.
    Safe to re-run: child scripts use IF NOT EXISTS / idempotent checks.

    Source scripts folder: Database/Scripts/AudioCaseIntelligenceV2/
    Generated: 2026-06-24
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;


/* =============================================================================
   STEP 0 | PHASE 0 | AudioCaseTaking_CreateTables.sql
   ============================================================================= */

/* ========== Phase 1: Core session ========== */
IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'AudioCaseSession' AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.AudioCaseSession
    (
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        PatientId BIGINT NOT NULL,
        CaseId BIGINT NULL,
        DoctorUserId BIGINT NOT NULL,
        PatientAppId BIGINT NULL,
        AudioSourceType NVARCHAR(20) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        CurrentStep NVARCHAR(50) NULL,
        AudioFilePath NVARCHAR(500) NULL,
        AudioFileName NVARCHAR(255) NULL,
        AudioMimeType NVARCHAR(100) NULL,
        AudioFileSizeBytes BIGINT NULL,
        AudioSha256Hash CHAR(64) NULL,
        AudioDurationSeconds INT NULL,
        TranscriptRaw NVARCHAR(MAX) NULL,
        ConversationJson NVARCHAR(MAX) NULL,
        SummaryJson NVARCHAR(MAX) NULL,
        ExtractedSymptomsJson NVARCHAR(MAX) NULL,
        SuggestedRubricsJson NVARCHAR(MAX) NULL,
        DetectedLanguage NVARCHAR(10) NULL,
        LanguageOverride NVARCHAR(10) NULL,
        CorrelationId NVARCHAR(50) NULL,
        ErrorCode NVARCHAR(50) NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        ReAnalysisCount INT NOT NULL CONSTRAINT DF_AudioCaseSession_ReAnalysisCount DEFAULT (0),
        EnteredBy INT NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_AudioCaseSession_EnteredDate DEFAULT (GETUTCDATE()),
        ChangedBy INT NULL,
        ChangedDate DATETIME NULL,
        CompletedAtUtc DATETIME NULL,
        AudioPurgedAtUtc DATETIME NULL,
        DeleteStatus BIT NOT NULL CONSTRAINT DF_AudioCaseSession_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_AudioCaseSession PRIMARY KEY CLUSTERED (AudioCaseSessionId)
    );

    CREATE NONCLUSTERED INDEX IX_AudioCaseSession_DoctorUserId_EnteredDate
        ON dbo.AudioCaseSession (DoctorUserId, EnteredDate DESC);

    CREATE NONCLUSTERED INDEX IX_AudioCaseSession_PatientId
        ON dbo.AudioCaseSession (PatientId, CaseId, EnteredDate DESC);
END
GO

IF COL_LENGTH('dbo.AudioCaseSession', 'LanguageOverride') IS NULL
    ALTER TABLE dbo.AudioCaseSession ADD LanguageOverride NVARCHAR(10) NULL;
GO
IF COL_LENGTH('dbo.AudioCaseSession', 'ReAnalysisCount') IS NULL
    ALTER TABLE dbo.AudioCaseSession ADD ReAnalysisCount INT NOT NULL CONSTRAINT DF_AudioCaseSession_ReAnalysisCount2 DEFAULT (0);
GO
IF COL_LENGTH('dbo.AudioCaseSession', 'AudioPurgedAtUtc') IS NULL
    ALTER TABLE dbo.AudioCaseSession ADD AudioPurgedAtUtc DATETIME NULL;
GO

/* ========== Phase 1: Event log ========== */
IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'AudioCaseSessionEventLog' AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.AudioCaseSessionEventLog
    (
        EventLogId BIGINT IDENTITY(1, 1) NOT NULL,
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        EventType NVARCHAR(80) NOT NULL,
        EventStatus NVARCHAR(20) NOT NULL,
        Message NVARCHAR(1000) NULL,
        DetailsJson NVARCHAR(MAX) NULL,
        DurationMs INT NULL,
        CorrelationId NVARCHAR(50) NULL,
        IpAddress NVARCHAR(45) NULL,
        EnteredBy INT NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_AudioCaseSessionEventLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseSessionEventLog PRIMARY KEY CLUSTERED (EventLogId),
        CONSTRAINT FK_AudioCaseSessionEventLog_Session
            FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );

    CREATE NONCLUSTERED INDEX IX_AudioCaseSessionEventLog_SessionId
        ON dbo.AudioCaseSessionEventLog (AudioCaseSessionId, EnteredDate);
END
GO

/* ========== Phase 1: AI request log ========== */
IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'AudioCaseAiRequestLog' AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.AudioCaseAiRequestLog
    (
        AiRequestLogId BIGINT IDENTITY(1, 1) NOT NULL,
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        Provider NVARCHAR(50) NOT NULL,
        ServiceType NVARCHAR(50) NOT NULL,
        ModelName NVARCHAR(100) NULL,
        RequestId NVARCHAR(100) NULL,
        HttpStatusCode INT NULL,
        PromptTokens INT NULL,
        CompletionTokens INT NULL,
        AudioDurationSeconds INT NULL,
        EstimatedCostUsd DECIMAL(10, 6) NULL,
        LatencyMs INT NULL,
        RequestPayloadHash CHAR(64) NULL,
        ResponsePayloadHash CHAR(64) NULL,
        RequestPayloadJson NVARCHAR(MAX) NULL,
        ResponsePayloadJson NVARCHAR(MAX) NULL,
        IsSuccess BIT NOT NULL,
        ErrorCode NVARCHAR(50) NULL,
        ErrorMessage NVARCHAR(2000) NULL,
        CorrelationId NVARCHAR(50) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_AudioCaseAiRequestLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseAiRequestLog PRIMARY KEY CLUSTERED (AiRequestLogId),
        CONSTRAINT FK_AudioCaseAiRequestLog_Session
            FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );

    CREATE NONCLUSTERED INDEX IX_AudioCaseAiRequestLog_SessionId
        ON dbo.AudioCaseAiRequestLog (AudioCaseSessionId, EnteredDate);
END
GO

/* ========== Phase 1: Consent log ========== */
IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'AudioCaseConsentLog' AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.AudioCaseConsentLog
    (
        ConsentLogId BIGINT IDENTITY(1, 1) NOT NULL,
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        PatientId BIGINT NOT NULL,
        DoctorUserId BIGINT NOT NULL,
        ConsentType NVARCHAR(50) NOT NULL,
        ConsentTextVersion NVARCHAR(20) NOT NULL,
        ConsentGiven BIT NOT NULL,
        IpAddress NVARCHAR(45) NULL,
        UserAgent NVARCHAR(500) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_AudioCaseConsentLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseConsentLog PRIMARY KEY CLUSTERED (ConsentLogId),
        CONSTRAINT FK_AudioCaseConsentLog_Session
            FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO

/* ========== Phase 2: Rubric match log ========== */
IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'AudioCaseRubricMatchLog' AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.AudioCaseRubricMatchLog
    (
        RubricMatchLogId BIGINT IDENTITY(1, 1) NOT NULL,
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        SymptomPhrase NVARCHAR(500) NOT NULL,
        SubSectionId INT NOT NULL,
        SubSectionName NVARCHAR(500) NULL,
        KeywordScore DECIMAL(5, 4) NULL,
        FullTextScore DECIMAL(5, 4) NULL,
        SemanticScore DECIMAL(5, 4) NULL,
        FinalScore DECIMAL(5, 4) NOT NULL,
        RankPosition INT NULL,
        IsSelectedForUi BIT NOT NULL CONSTRAINT DF_AudioCaseRubricMatchLog_IsSelected DEFAULT (0),
        SuggestedIntensityNo INT NULL,
        MatchSource NVARCHAR(50) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_AudioCaseRubricMatchLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseRubricMatchLog PRIMARY KEY CLUSTERED (RubricMatchLogId),
        CONSTRAINT FK_AudioCaseRubricMatchLog_Session
            FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );

    CREATE NONCLUSTERED INDEX IX_AudioCaseRubricMatchLog_SessionId
        ON dbo.AudioCaseRubricMatchLog (AudioCaseSessionId, FinalScore DESC);
END
GO

/* ========== Phase 2: Doctor action log ========== */
IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'AudioCaseDoctorActionLog' AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.AudioCaseDoctorActionLog
    (
        DoctorActionLogId BIGINT IDENTITY(1, 1) NOT NULL,
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        DoctorUserId BIGINT NOT NULL,
        ActionType NVARCHAR(80) NOT NULL,
        TargetType NVARCHAR(50) NULL,
        TargetId NVARCHAR(100) NULL,
        BeforeJson NVARCHAR(MAX) NULL,
        AfterJson NVARCHAR(MAX) NULL,
        Notes NVARCHAR(500) NULL,
        IpAddress NVARCHAR(45) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_AudioCaseDoctorActionLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseDoctorActionLog PRIMARY KEY CLUSTERED (DoctorActionLogId),
        CONSTRAINT FK_AudioCaseDoctorActionLog_Session
            FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );

    CREATE NONCLUSTERED INDEX IX_AudioCaseDoctorActionLog_SessionId
        ON dbo.AudioCaseDoctorActionLog (AudioCaseSessionId, EnteredDate);
END
GO

/* ========== Phase 2/3: Retention log ========== */
IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'AudioCaseRetentionLog' AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.AudioCaseRetentionLog
    (
        RetentionLogId BIGINT IDENTITY(1, 1) NOT NULL,
        AudioCaseSessionId UNIQUEIDENTIFIER NOT NULL,
        ActionType NVARCHAR(50) NOT NULL,
        Reason NVARCHAR(200) NULL,
        PerformedBy NVARCHAR(50) NOT NULL,
        DetailsJson NVARCHAR(MAX) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_AudioCaseRetentionLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseRetentionLog PRIMARY KEY CLUSTERED (RetentionLogId),
        CONSTRAINT FK_AudioCaseRetentionLog_Session
            FOREIGN KEY (AudioCaseSessionId) REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO


/* =============================================================================
   STEP 1 | PHASE 1 | 001_Create_AudioCaseClinicalConcept.sql
   ============================================================================= */

-- Phase 1: Clinical concepts extracted by Case Understanding Engine
-- Manual deploy only â€” do not auto-apply

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AudioCaseClinicalConcept' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AudioCaseClinicalConcept
    (
        ConceptId            UNIQUEIDENTIFIER NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        RawStatement         NVARCHAR(1000) NOT NULL,
        ClinicalMeaning      NVARCHAR(2000) NULL,
        HomeopathicMeaning   NVARCHAR(2000) NULL,
        Category             NVARCHAR(50) NULL,
        IsSRP                BIT NOT NULL CONSTRAINT DF_AudioCaseClinicalConcept_IsSRP DEFAULT (0),
        ModalitiesJson       NVARCHAR(MAX) NULL,
        ConcomitantsJson     NVARCHAR(MAX) NULL,
        SequenceJson         NVARCHAR(MAX) NULL,
        Confidence           DECIMAL(5,4) NOT NULL,
        SourceLanguage       NVARCHAR(10) NULL,
        EnteredDate          DATETIME NOT NULL CONSTRAINT DF_AudioCaseClinicalConcept_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseClinicalConcept PRIMARY KEY (ConceptId),
        CONSTRAINT FK_AudioCaseClinicalConcept_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO


/* =============================================================================
   STEP 2 | PHASE 1 | 002_Create_AudioCaseIntelligenceLog.sql
   ============================================================================= */

-- Per-stage audit log for V2 intelligence pipeline

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AudioCaseIntelligenceLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AudioCaseIntelligenceLog
    (
        IntelligenceLogId    BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        CorrelationId        NVARCHAR(32) NULL,
        StageName            NVARCHAR(100) NOT NULL,
        Status               NVARCHAR(30) NOT NULL,
        Message              NVARCHAR(2000) NULL,
        DetailsJson          NVARCHAR(MAX) NULL,
        LatencyMs            INT NULL,
        EngineVersion        NVARCHAR(10) NOT NULL CONSTRAINT DF_AudioCaseIntelligenceLog_EngineVersion DEFAULT ('v2'),
        EnteredDate          DATETIME NOT NULL CONSTRAINT DF_AudioCaseIntelligenceLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseIntelligenceLog PRIMARY KEY (IntelligenceLogId),
        CONSTRAINT FK_AudioCaseIntelligenceLog_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_AudioCaseIntelligenceLog_SessionId
    ON dbo.AudioCaseIntelligenceLog (AudioCaseSessionId, EnteredDate DESC);
GO


/* =============================================================================
   STEP 3 | PHASE 1 | 102_Alter_AudioCaseSession_V2Columns.sql
   ============================================================================= */

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


/* =============================================================================
   STEP 4 | PHASE 2 | 003_Create_RubricMetaphorDictionary.sql
   ============================================================================= */

-- Patient expression â†’ clinical/rubric meaning mappings

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricMetaphorDictionary' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricMetaphorDictionary
    (
        MetaphorId           BIGINT IDENTITY(1,1) NOT NULL,
        PatientExpression    NVARCHAR(500) NOT NULL,
        NormalizedExpression NVARCHAR(500) NOT NULL,
        ClinicalMeaning      NVARCHAR(1000) NOT NULL,
        RubricMeaning        NVARCHAR(500) NOT NULL,
        SubSectionId         INT NULL,
        Language             NVARCHAR(10) NOT NULL,
        ConfidenceWeight     DECIMAL(5,4) NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_ConfidenceWeight DEFAULT (0.85),
        ApprovalStatus       NVARCHAR(20) NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_ApprovalStatus DEFAULT ('Pending'),
        UsageCount           INT NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_UsageCount DEFAULT (0),
        AcceptanceRate       DECIMAL(5,4) NULL,
        VersionNo            INT NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_VersionNo DEFAULT (1),
        IsActive             BIT NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_IsActive DEFAULT (1),
        EnteredBy            INT NULL,
        EnteredDate          DATETIME NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_EnteredDate DEFAULT (GETUTCDATE()),
        ApprovedBy           INT NULL,
        ApprovedDate         DATETIME NULL,
        CONSTRAINT PK_RubricMetaphorDictionary PRIMARY KEY (MetaphorId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_RubricMetaphorDictionary_Normalized
    ON dbo.RubricMetaphorDictionary (NormalizedExpression, Language)
    WHERE IsActive = 1;
GO


/* =============================================================================
   STEP 5 | PHASE 2 | 004_Create_RubricAlias.sql
   ============================================================================= */

-- Alternate search terms mapped to SubSectionMaster rubrics

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricAlias' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricAlias
    (
        RubricAliasId     BIGINT IDENTITY(1,1) NOT NULL,
        SubSectionId      INT NOT NULL,
        AliasText         NVARCHAR(500) NOT NULL,
        NormalizedAlias   NVARCHAR(500) NOT NULL,
        Language          NVARCHAR(10) NOT NULL,
        AliasType         NVARCHAR(50) NOT NULL,
        Weight            DECIMAL(5,4) NOT NULL CONSTRAINT DF_RubricAlias_Weight DEFAULT (1.0),
        Source            NVARCHAR(50) NOT NULL,
        UsageCount        INT NOT NULL CONSTRAINT DF_RubricAlias_UsageCount DEFAULT (0),
        AcceptanceRate    DECIMAL(5,4) NULL,
        IsActive          BIT NOT NULL CONSTRAINT DF_RubricAlias_IsActive DEFAULT (1),
        VersionNo         INT NOT NULL CONSTRAINT DF_RubricAlias_VersionNo DEFAULT (1),
        EnteredBy         INT NULL,
        EnteredDate       DATETIME NOT NULL CONSTRAINT DF_RubricAlias_EnteredDate DEFAULT (GETUTCDATE()),
        ChangedBy         INT NULL,
        ChangedDate       DATETIME NULL,
        CONSTRAINT PK_RubricAlias PRIMARY KEY (RubricAliasId),
        CONSTRAINT FK_RubricAlias_SubSection FOREIGN KEY (SubSectionId)
            REFERENCES dbo.SubSectionMaster (SubSectionId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_RubricAlias_Normalized
    ON dbo.RubricAlias (NormalizedAlias)
    WHERE IsActive = 1;
GO

CREATE NONCLUSTERED INDEX IX_RubricAlias_SubSectionId ON dbo.RubricAlias (SubSectionId);
GO


/* =============================================================================
   STEP 6 | PHASE 2 | 014_Create_RubricAdminAuditLog.sql
   ============================================================================= */

-- Admin audit trail for metaphor/alias CRUD

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricAdminAuditLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricAdminAuditLog
    (
        AuditLogId     BIGINT IDENTITY(1,1) NOT NULL,
        EntityType     NVARCHAR(50) NOT NULL,
        EntityId       BIGINT NOT NULL,
        ActionType     NVARCHAR(30) NOT NULL,
        BeforeJson     NVARCHAR(MAX) NULL,
        AfterJson      NVARCHAR(MAX) NULL,
        AdminUserId    INT NOT NULL,
        IpAddress      NVARCHAR(45) NULL,
        EnteredDate    DATETIME NOT NULL CONSTRAINT DF_RubricAdminAuditLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_RubricAdminAuditLog PRIMARY KEY (AuditLogId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_RubricAdminAuditLog_Entity
    ON dbo.RubricAdminAuditLog (EntityType, EntityId, EnteredDate DESC);
GO


/* =============================================================================
   STEP 7 | PHASE 2 | 501_Seed_MetaphorDictionary_EN_HI_MR.sql
   ============================================================================= */

-- Initial metaphor seed (EN + HI + MR samples)
-- Run AFTER 003_Create_RubricMetaphorDictionary.sql

IF NOT EXISTS (SELECT 1 FROM dbo.RubricMetaphorDictionary WHERE NormalizedExpression = N'vibration before fit')
BEGIN
    INSERT INTO dbo.RubricMetaphorDictionary
        (PatientExpression, NormalizedExpression, ClinicalMeaning, RubricMeaning, Language, ConfidenceWeight, ApprovalStatus, IsActive)
    VALUES
        (N'vibration before fit', N'vibration before fit', N'Prodromal aura before convulsive episode', N'GENERALITIES - CONVULSIONS - aura', N'en', 0.92, N'Approved', 1),
        (N'vibration in hands before attack', N'vibration in hands before attack', N'Sensory aura in hands preceding seizure', N'GENERALITIES - CONVULSIONS - aura', N'en', 0.90, N'Approved', 1),
        (N'dropping things from hands before fit', N'dropping things from hands before fit', N'Motor aura â€” dropping objects before convulsion', N'EXTREMITIES - HAND - dropping things', N'en', 0.88, N'Approved', 1),
        (N'fits mostly at night', N'fits mostly at night', N'Nocturnal convulsive tendency', N'GENERALITIES - CONVULSIONS - night', N'en', 0.85, N'Approved', 1),
        (N'fear of darkness since childhood', N'fear of darkness since childhood', N'Phobia of dark â€” mental symptom', N'MIND - FEAR - dark', N'en', 0.87, N'Approved', 1),
        (N'ailments from grief', N'ailments from grief', N'Causation from bereavement', N'MIND - GRIEF - ailments from', N'en', 0.86, N'Approved', 1);
END
GO

-- Marathi patient expressions (translated clinical meaning in English)
IF NOT EXISTS (SELECT 1 FROM dbo.RubricMetaphorDictionary WHERE Language = N'mr' AND NormalizedExpression = N'hatat hath kamptat')
BEGIN
    INSERT INTO dbo.RubricMetaphorDictionary
        (PatientExpression, NormalizedExpression, ClinicalMeaning, RubricMeaning, Language, ConfidenceWeight, ApprovalStatus, IsActive)
    VALUES
        (N'à¤¹à¤¾à¤¤à¤¾à¤¤ à¤…à¤šà¤¾à¤¨à¤• à¤•à¤‚à¤ª à¤¸à¥à¤°à¥‚ à¤¹à¥‹à¤¤à¥‡', N'hatat achanak kamp suru hote', N'Sudden trembling in hands â€” prodromal aura', N'GENERALITIES - CONVULSIONS - aura', N'mr', 0.90, N'Approved', 1),
        (N'à¤«à¤¿à¤Ÿ à¤¯à¥‡à¤£à¥à¤¯à¤¾à¤ªà¥‚à¤°à¥à¤µà¥€ à¤¹à¤¾à¤¤ à¤•à¤¾à¤ªà¤¤à¤¾à¤¤', N'fit yenya purvi hat kapdat', N'Hands shake before convulsion', N'GENERALITIES - CONVULSIONS - aura', N'mr', 0.91, N'Approved', 1);
END
GO

-- Hindi patient expressions
IF NOT EXISTS (SELECT 1 FROM dbo.RubricMetaphorDictionary WHERE Language = N'hi' AND NormalizedExpression = N'daura se pehle kanpna')
BEGIN
    INSERT INTO dbo.RubricMetaphorDictionary
        (PatientExpression, NormalizedExpression, ClinicalMeaning, RubricMeaning, Language, ConfidenceWeight, ApprovalStatus, IsActive)
    VALUES
        (N'à¤¦à¥Œà¤°à¥‡ à¤¸à¥‡ à¤ªà¤¹à¤²à¥‡ à¤¹à¤¾à¤¥ à¤•à¤¾à¤‚à¤ªà¤¤à¥‡ à¤¹à¥ˆà¤‚', N'daure se pehle hath kampate hain', N'Trembling hands before seizure â€” aura', N'GENERALITIES - CONVULSIONS - aura', N'hi', 0.91, N'Approved', 1),
        (N'andhere se bahut darr lagta hai', N'andhere se bahut darr lagta hai', N'Intense fear of darkness', N'MIND - FEAR - dark', N'hi', 0.86, N'Approved', 1);
END
GO

PRINT 'Metaphor seed batch 001 applied.';
GO


/* =============================================================================
   STEP 8 | PHASE 2 | 502_Seed_RubricAlias_Batch001.sql
   ============================================================================= */

-- Initial alias seed â€” links patient phrases to SubSectionMaster rubrics
-- Run AFTER 004_Create_RubricAlias.sql
-- Uses dynamic lookup so SubSectionId resolves from your database rubric names

DECLARE @AuraSubSectionId INT =
    (SELECT TOP 1 SubSectionId FROM dbo.SubSectionMaster
     WHERE SubSectionName LIKE N'%CONVULSIONS%AURA%' OR SubSectionName LIKE N'%CONVULSION%AURA%'
     ORDER BY SubSectionId);

DECLARE @FearDarkSubSectionId INT =
    (SELECT TOP 1 SubSectionId FROM dbo.SubSectionMaster
     WHERE SubSectionName LIKE N'%FEAR%DARK%' OR SubSectionName LIKE N'%MIND%FEAR%dark%'
     ORDER BY SubSectionId);

IF @AuraSubSectionId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.RubricAlias WHERE NormalizedAlias = N'vibration before fit')
BEGIN
    INSERT INTO dbo.RubricAlias (SubSectionId, AliasText, NormalizedAlias, Language, AliasType, Weight, Source, IsActive)
    VALUES
        (@AuraSubSectionId, N'vibration before fit', N'vibration before fit', N'en', N'patient_phrase', 0.95, N'seed', 1),
        (@AuraSubSectionId, N'vibration in hands before attack', N'vibration in hands before attack', N'en', N'patient_phrase', 0.92, N'seed', 1),
        (@AuraSubSectionId, N'warning before convulsion', N'warning before convulsion', N'en', N'clinical', 0.90, N'seed', 1),
        (@AuraSubSectionId, N'prodromal aura', N'prodromal aura', N'en', N'clinical', 0.88, N'seed', 1);
END
GO

DECLARE @FearDarkId INT =
    (SELECT TOP 1 SubSectionId FROM dbo.SubSectionMaster
     WHERE SubSectionName LIKE N'%FEAR%DARK%' ORDER BY SubSectionId);

IF @FearDarkId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.RubricAlias WHERE NormalizedAlias = N'fear of darkness')
BEGIN
    INSERT INTO dbo.RubricAlias (SubSectionId, AliasText, NormalizedAlias, Language, AliasType, Weight, Source, IsActive)
    VALUES
        (@FearDarkId, N'fear of darkness', N'fear of darkness', N'en', N'patient_phrase', 0.90, N'seed', 1),
        (@FearDarkId, N'scared of dark', N'scared of dark', N'en', N'patient_phrase', 0.85, N'seed', 1);
END
GO

PRINT 'Alias seed batch 001 applied (skipped rows when rubrics not found in SubSectionMaster).';
GO


/* =============================================================================
   STEP 9 | PHASE 3 | 006_Create_HomeopathicWeightRule.sql
   ============================================================================= */

-- Configurable homeopathic hierarchy weights for rubric scoring

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HomeopathicWeightRule' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HomeopathicWeightRule
    (
        WeightRuleId    INT IDENTITY(1,1) NOT NULL,
        RuleCode        NVARCHAR(50) NOT NULL,
        Category        NVARCHAR(50) NOT NULL,
        WeightValue     DECIMAL(5,2) NOT NULL,
        Description     NVARCHAR(500) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_HomeopathicWeightRule_IsActive DEFAULT (1),
        EnteredDate     DATETIME NOT NULL CONSTRAINT DF_HomeopathicWeightRule_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_HomeopathicWeightRule PRIMARY KEY (WeightRuleId),
        CONSTRAINT UQ_HomeopathicWeightRule_RuleCode UNIQUE (RuleCode)
    );
END
GO


/* =============================================================================
   STEP 10 | PHASE 3 | 007_Create_AudioCaseCausationLink.sql
   ============================================================================= */

-- Causation chains detected per audio case session

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AudioCaseCausationLink' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AudioCaseCausationLink
    (
        CausationLinkId      UNIQUEIDENTIFIER NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        CauseConceptId       UNIQUEIDENTIFIER NULL,
        EffectConceptId      UNIQUEIDENTIFIER NULL,
        CauseText            NVARCHAR(500) NOT NULL,
        EffectText           NVARCHAR(500) NOT NULL,
        LinkType             NVARCHAR(30) NOT NULL CONSTRAINT DF_AudioCaseCausationLink_LinkType DEFAULT ('CauseEffect'),
        Confidence           DECIMAL(5,4) NOT NULL,
        SequenceOrder        INT NOT NULL CONSTRAINT DF_AudioCaseCausationLink_SequenceOrder DEFAULT (0),
        EnteredDate          DATETIME NOT NULL CONSTRAINT DF_AudioCaseCausationLink_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseCausationLink PRIMARY KEY (CausationLinkId),
        CONSTRAINT FK_AudioCaseCausationLink_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_AudioCaseCausationLink_SessionId
    ON dbo.AudioCaseCausationLink (AudioCaseSessionId, SequenceOrder);
GO


/* =============================================================================
   STEP 11 | PHASE 3 | 103_Alter_AudioCaseSession_CausationLinksJson.sql
   ============================================================================= */

-- Phase 3: Causation links JSON cache on session (for GET /concepts without DB join)

IF COL_LENGTH('dbo.AudioCaseSession', 'CausationLinksJson') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession
        ADD CausationLinksJson NVARCHAR(MAX) NULL;
END
GO


/* =============================================================================
   STEP 12 | PHASE 3 | 503_Seed_HomeopathicWeightRule.sql
   ============================================================================= */

-- Default homeopathic weight hierarchy
-- Run AFTER 006_Create_HomeopathicWeightRule.sql

MERGE dbo.HomeopathicWeightRule AS target
USING (VALUES
    (N'SRP',              N'srp',          10.00, N'Strange, rare, peculiar symptoms'),
    (N'MENTAL',           N'mental',        8.00, N'Mental/emotional symptoms'),
    (N'CAUSATION',        N'causation',     7.00, N'Clear causation chain'),
    (N'GENERAL',          N'general',       5.00, N'General symptoms'),
    (N'PARTICULAR',       N'particular',    4.00, N'Particular/local symptoms'),
    (N'CONFIRMATORY',     N'confirmatory',  2.00, N'Confirmatory rubrics'),
    (N'CONCOMITANT',      N'concomitant',   3.50, N'Concomitant symptoms')
) AS source (RuleCode, Category, WeightValue, Description)
ON target.RuleCode = source.RuleCode
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RuleCode, Category, WeightValue, Description, IsActive)
    VALUES (source.RuleCode, source.Category, source.WeightValue, source.Description, 1);
GO

PRINT 'Homeopathic weight rules seeded.';
GO


/* =============================================================================
   STEP 13 | PHASE 4 | 005_Create_RubricEmbeddings.sql
   ============================================================================= */

-- JSON embedding vectors (not SQL Server native vector type)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricEmbeddings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricEmbeddings
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        RubricId        INT NOT NULL,
        EmbeddingJson   NVARCHAR(MAX) NOT NULL,
        ModelName       NVARCHAR(100) NOT NULL,
        TextHash        CHAR(64) NOT NULL,
        SourceType      NVARCHAR(30) NOT NULL,
        CreatedDate     DATETIME NOT NULL CONSTRAINT DF_RubricEmbeddings_CreatedDate DEFAULT (GETUTCDATE()),
        UpdatedDate     DATETIME NULL,
        CONSTRAINT PK_RubricEmbeddings PRIMARY KEY (Id),
        CONSTRAINT FK_RubricEmbeddings_SubSection FOREIGN KEY (RubricId)
            REFERENCES dbo.SubSectionMaster (SubSectionId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_RubricEmbeddings_RubricId ON dbo.RubricEmbeddings (RubricId);
GO

CREATE NONCLUSTERED INDEX IX_RubricEmbeddings_TextHash ON dbo.RubricEmbeddings (TextHash);
GO


/* =============================================================================
   STEP 14 | PHASE 4 | 201_Indexes_All.sql
   ============================================================================= */

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


/* =============================================================================
   STEP 15 | PHASE 5 | 008_Create_AudioCaseClinicalInferenceLog.sql
   ============================================================================= */

-- Phase 5: Clinical inference audit log

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AudioCaseClinicalInferenceLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AudioCaseClinicalInferenceLog
    (
        InferenceLogId       BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        SourceConceptId      UNIQUEIDENTIFIER NULL,
        InferredRubricName   NVARCHAR(500) NOT NULL,
        SubSectionId         INT NULL,
        Reason               NVARCHAR(2000) NOT NULL,
        SourceSymptom        NVARCHAR(500) NULL,
        Confidence           DECIMAL(5,4) NOT NULL,
        DoctorAccepted       BIT NULL,
        EnteredDate          DATETIME NOT NULL CONSTRAINT DF_AudioCaseClinicalInferenceLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseClinicalInferenceLog PRIMARY KEY (InferenceLogId),
        CONSTRAINT FK_AudioCaseClinicalInferenceLog_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_AudioCaseClinicalInferenceLog_SessionId
    ON dbo.AudioCaseClinicalInferenceLog (AudioCaseSessionId, EnteredDate DESC);
GO

PRINT 'AudioCaseClinicalInferenceLog table ready.';
GO


/* =============================================================================
   STEP 16 | PHASE 5 | 101_Alter_AudioCaseRubricMatchLog_V2Columns.sql
   ============================================================================= */

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


/* =============================================================================
   STEP 17 | PHASE 6 | 009_Create_AudioCaseRubricFeedback.sql
   ============================================================================= */

-- Phase 6: Doctor rubric feedback (accept / reject / correct)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AudioCaseRubricFeedback' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AudioCaseRubricFeedback
    (
        FeedbackId              BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId      UNIQUEIDENTIFIER NOT NULL,
        SubSectionId            INT NULL,
        RubricName              NVARCHAR(500) NOT NULL,
        FeedbackType            NVARCHAR(30) NOT NULL,
        OriginalMatchLayer      NVARCHAR(50) NULL,
        CorrectedSubSectionId   INT NULL,
        Reason                  NVARCHAR(1000) NULL,
        ConfidenceAtFeedback    DECIMAL(5,4) NULL,
        EngineVersion           NVARCHAR(10) NOT NULL,
        DoctorUserId            BIGINT NOT NULL,
        EnteredDate             DATETIME NOT NULL CONSTRAINT DF_AudioCaseRubricFeedback_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseRubricFeedback PRIMARY KEY (FeedbackId),
        CONSTRAINT FK_AudioCaseRubricFeedback_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioCaseRubricFeedback_Session'
      AND object_id = OBJECT_ID('dbo.AudioCaseRubricFeedback'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AudioCaseRubricFeedback_Session
        ON dbo.AudioCaseRubricFeedback (AudioCaseSessionId, EnteredDate DESC);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioCaseRubricFeedback_SubSection'
      AND object_id = OBJECT_ID('dbo.AudioCaseRubricFeedback'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AudioCaseRubricFeedback_SubSection
        ON dbo.AudioCaseRubricFeedback (SubSectionId, FeedbackType)
        WHERE SubSectionId IS NOT NULL;
END
GO


/* =============================================================================
   STEP 18 | PHASE 6 | 010_Create_AudioCaseRubricBenchmark.sql
   ============================================================================= */

-- Phase 6: Per-session rubric intelligence benchmark metrics

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AudioCaseRubricBenchmark' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AudioCaseRubricBenchmark
    (
        BenchmarkId             BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId      UNIQUEIDENTIFIER NOT NULL,
        EngineVersion           NVARCHAR(10) NOT NULL,
        AiSuggestedCount        INT NOT NULL,
        DoctorAcceptedCount     INT NOT NULL,
        DoctorRejectedCount     INT NOT NULL,
        DoctorCorrectedCount    INT NOT NULL,
        PrimaryInTop5           BIT NULL,
        PrecisionScore          DECIMAL(5,4) NULL,
        RecallScore             DECIMAL(5,4) NULL,
        F1Score                 DECIMAL(5,4) NULL,
        AcceptanceRate          DECIMAL(5,4) NULL,
        FalsePositiveRate       DECIMAL(5,4) NULL,
        ConfidenceCalibration   DECIMAL(5,4) NULL,
        CalculatedDate          DATETIME NOT NULL CONSTRAINT DF_AudioCaseRubricBenchmark_CalculatedDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseRubricBenchmark PRIMARY KEY (BenchmarkId),
        CONSTRAINT FK_AudioCaseRubricBenchmark_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioCaseRubricBenchmark_Session'
      AND object_id = OBJECT_ID('dbo.AudioCaseRubricBenchmark'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_AudioCaseRubricBenchmark_Session
        ON dbo.AudioCaseRubricBenchmark (AudioCaseSessionId);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioCaseRubricBenchmark_CalculatedDate'
      AND object_id = OBJECT_ID('dbo.AudioCaseRubricBenchmark'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AudioCaseRubricBenchmark_CalculatedDate
        ON dbo.AudioCaseRubricBenchmark (CalculatedDate DESC, EngineVersion);
END
GO


/* =============================================================================
   STEP 19 | PHASE 6 | 015_Create_GoldCaseLibrary.sql
   ============================================================================= */

-- Internal gold standard benchmark cases (250+ target)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GoldCaseLibrary' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.GoldCaseLibrary
    (
        GoldCaseId              BIGINT IDENTITY(1,1) NOT NULL,
        Category                NVARCHAR(50) NOT NULL,
        Transcript              NVARCHAR(MAX) NOT NULL,
        SourceLanguage          NVARCHAR(10) NULL,
        DoctorRubricsJson       NVARCHAR(MAX) NOT NULL,
        PrimaryRubricIdsJson    NVARCHAR(MAX) NOT NULL,
        FinalRemedy             NVARCHAR(200) NULL,
        FollowUpOutcome         NVARCHAR(1000) NULL,
        ReviewedBy              INT NULL,
        ReviewDate              DATETIME NULL,
        IsActive                BIT NOT NULL CONSTRAINT DF_GoldCaseLibrary_IsActive DEFAULT (1),
        EnteredDate             DATETIME NOT NULL CONSTRAINT DF_GoldCaseLibrary_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_GoldCaseLibrary PRIMARY KEY (GoldCaseId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_GoldCaseLibrary_Category
    ON dbo.GoldCaseLibrary (Category)
    WHERE IsActive = 1;
GO


/* =============================================================================
   STEP 20 | PHASE 6 | 504_Seed_GoldCaseLibrary_Batch001.sql
   ============================================================================= */

-- Phase 6: Seed starter gold benchmark cases (expand to 250+ over time)

IF NOT EXISTS (SELECT 1 FROM dbo.GoldCaseLibrary WHERE Category = N'Epilepsy' AND Transcript LIKE N'%Vibration in hands%')
BEGIN
    INSERT INTO dbo.GoldCaseLibrary (Category, Transcript, SourceLanguage, DoctorRubricsJson, PrimaryRubricIdsJson, FinalRemedy, IsActive)
    VALUES
    (
        N'Epilepsy',
        N'Vibration in hands 10 seconds before every fit at night.',
        N'en',
        N'["aura before convulsion","vibration hands"]',
        N'[]',
        NULL,
        1
    ),
    (
        N'Psychological',
        N'Since father''s death she cannot sleep and weeps alone.',
        N'en',
        N'["grief","weeping","sleep disturbed"]',
        N'[]',
        NULL,
        1
    ),
    (
        N'Gastrointestinal',
        N'Burning pain in stomach worse after eating spicy food.',
        N'en',
        N'["burning stomach","eating agg"]',
        N'[]',
        NULL,
        1
    ),
    (
        N'Pediatric',
        N'Child screams before urination with red face.',
        N'en',
        N'["screaming before urination","red face"]',
        N'[]',
        NULL,
        1
    ),
    (
        N'Chronic',
        N'Joint pain worse in damp weather, better by warmth.',
        N'en',
        N'["joint pain damp agg","warm amel"]',
        N'[]',
        NULL,
        1
    );
END
GO

/* =============================================================================
   DEPLOYMENT COMPLETE — Phases 0–6
   ============================================================================= */
PRINT 'HomeoCentrum V2 Phases 0–6 deployment script finished successfully.';
GO

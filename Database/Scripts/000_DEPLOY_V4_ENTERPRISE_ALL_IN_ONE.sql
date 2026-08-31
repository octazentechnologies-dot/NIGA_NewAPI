/*
================================================================================
  HOMEOCENTRUM — V4 ENTERPRISE AI PIPELINE
  SINGLE FILE — ALL SQL IN ONE SCRIPT (no external files needed)

  Copy this ONE file to your PC, open in SSMS, connect to your server, run F5.

  Target database: HomeoCentrum_Production (change USE line below if different)
  Safe to re-run:  uses IF NOT EXISTS everywhere
  Does NOT modify repertory master tables (SectionMaster, SubSectionMaster, etc.)

  BEFORE YOU RUN:
  1. Take a FULL backup of your database
  2. Uncomment USE database line below
  3. Execute entire script (F5) — takes 2-10 minutes

  AFTER SQL:
  - Deploy New_API (publish to server)
  - Deploy NigaHomeopathy-UI (npm run build)
  - Run post-deploy API calls (embeddings build, KG bootstrap) — see deployment guide
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- >>> CHANGE DATABASE NAME HERE IF NEEDED:
USE HomeoCentrum_Production;
GO

PRINT '================================================================';
PRINT ' V4 ENTERPRISE ALL-IN-ONE SQL DEPLOY — START';
PRINT ' ' + CONVERT(VARCHAR(30), GETUTCDATE(), 120) + ' UTC';
PRINT '================================================================';
GO

/* ==============================================================================
   SECTION: PREREQ — Audio Case Taking V1
   ============================================================================== */
PRINT '';
PRINT '>>> PREREQ — Audio Case Taking V1';
GO

/*
    HomeoCentrum - Audio case taking (all phases)
    Database: HomeoCentrum_Production
    Run manually before deploying AudioCaseTaking API.
    Safe to re-run: uses IF NOT EXISTS / column-existence checks.
*/

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

GO

/* ==============================================================================
   SECTION: V2 Phases 0-6
   ============================================================================== */
PRINT '';
PRINT '>>> V2 Phases 0-6';
GO

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

GO

/* ==============================================================================
   SECTION: V2 Phase 7 Repertory
   ============================================================================== */
PRINT '';
PRINT '>>> V2 Phase 7 Repertory';
GO

/* Phase 7 — Repertory Kent + Complete. Run AFTER 000_DEPLOY_ALL_Phases_0_to_6.sql */
SET NOCOUNT ON; SET XACT_ABORT ON;

/* STEP 21 | 011_Create_RepertorySource.sql */
-- Phase 7: Repertory source registry (Kent, Complete, future Boericke/Phatak)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RepertorySource' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RepertorySource
    (
        RepertorySourceId   BIGINT IDENTITY(1,1) NOT NULL,
        SourceCode          NVARCHAR(20) NOT NULL,
        SourceName          NVARCHAR(100) NOT NULL,
        AuthorId            INT NULL,
        PriorityOrder       INT NOT NULL CONSTRAINT DF_RepertorySource_PriorityOrder DEFAULT (100),
        IsActive            BIT NOT NULL CONSTRAINT DF_RepertorySource_IsActive DEFAULT (1),
        EnteredDate         DATETIME NOT NULL CONSTRAINT DF_RepertorySource_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_RepertorySource PRIMARY KEY (RepertorySourceId),
        CONSTRAINT UQ_RepertorySource_SourceCode UNIQUE (SourceCode)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RepertorySource_ActivePriority'
      AND object_id = OBJECT_ID('dbo.RepertorySource'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RepertorySource_ActivePriority
        ON dbo.RepertorySource (IsActive, PriorityOrder)
        INCLUDE (SourceCode, SourceName);
END
GO

/* STEP 22 | 012_Create_RubricRepertoryMap.sql */
-- Phase 7: Map SubSectionMaster rubrics to repertory sources (Kent / Complete)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricRepertoryMap' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricRepertoryMap
    (
        RubricRepertoryMapId  BIGINT IDENTITY(1,1) NOT NULL,
        SubSectionId          INT NOT NULL,
        RepertorySourceId     BIGINT NOT NULL,
        SourceRubricKey       NVARCHAR(2000) NULL,
        SourceRubricPath      NVARCHAR(1000) NULL,
        MappingConfidence     DECIMAL(5,4) NOT NULL CONSTRAINT DF_RubricRepertoryMap_Confidence DEFAULT (1.0000),
        IsPrimarySource       BIT NOT NULL CONSTRAINT DF_RubricRepertoryMap_IsPrimary DEFAULT (0),
        IsActive              BIT NOT NULL CONSTRAINT DF_RubricRepertoryMap_IsActive DEFAULT (1),
        EnteredDate           DATETIME NOT NULL CONSTRAINT DF_RubricRepertoryMap_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_RubricRepertoryMap PRIMARY KEY (RubricRepertoryMapId),
        CONSTRAINT FK_RubricRepertoryMap_SubSection FOREIGN KEY (SubSectionId)
            REFERENCES dbo.SubSectionMaster (SubSectionId),
        CONSTRAINT FK_RubricRepertoryMap_Source FOREIGN KEY (RepertorySourceId)
            REFERENCES dbo.RepertorySource (RepertorySourceId),
        CONSTRAINT UQ_RubricRepertoryMap_SubSectionSource UNIQUE (SubSectionId, RepertorySourceId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RubricRepertoryMap_SubSection'
      AND object_id = OBJECT_ID('dbo.RubricRepertoryMap'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricRepertoryMap_SubSection
        ON dbo.RubricRepertoryMap (SubSectionId)
        WHERE IsActive = 1;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RubricRepertoryMap_Source'
      AND object_id = OBJECT_ID('dbo.RubricRepertoryMap'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricRepertoryMap_Source
        ON dbo.RubricRepertoryMap (RepertorySourceId)
        WHERE IsActive = 1;
END
GO

/* STEP 23 | 013_Alter_RubricRepertoryMap_WidenColumns.sql */
-- Phase 7.1: Widen RubricRepertoryMap text columns
-- SubSectionMaster.SubSectionName can exceed 200 chars (hierarchical Kent rubric paths)

IF OBJECT_ID('dbo.RubricRepertoryMap', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.RubricRepertoryMap')
          AND name = 'SourceRubricKey'
          AND max_length < 4000)
    BEGIN
        ALTER TABLE dbo.RubricRepertoryMap ALTER COLUMN SourceRubricKey NVARCHAR(2000) NULL;
        PRINT 'Widened RubricRepertoryMap.SourceRubricKey to NVARCHAR(2000).';
    END

    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.RubricRepertoryMap')
          AND name = 'SourceRubricPath'
          AND max_length < 2000)
    BEGIN
        ALTER TABLE dbo.RubricRepertoryMap ALTER COLUMN SourceRubricPath NVARCHAR(1000) NULL;
        PRINT 'Widened RubricRepertoryMap.SourceRubricPath to NVARCHAR(1000).';
    END
END
GO

/* STEP 24 | 505_Seed_RepertorySource_Kent_Complete.sql */
-- Phase 7: Seed Kent + Complete repertory sources and map rubrics from AuthorMaster links
-- Run AFTER 011, 012, and 013 (013 widens SourceRubricKey â€” required on existing DBs)
-- Maps ALL rubrics linked to ANY Kent / Complete author row (not just TOP 1 AuthorId)

DECLARE @KentPrimaryAuthorId INT =
    (SELECT TOP 1 AuthorId FROM dbo.AuthorMaster
     WHERE ISNULL(IsDeleted, 0) = 0
       AND AuthorName LIKE N'%KENT%'
       AND AuthorName NOT LIKE N'%Complete%'
       AND AuthorName NOT LIKE N'%COMPAR%'
     ORDER BY CASE WHEN AuthorName LIKE N'KENT J.%' THEN 0 ELSE 1 END, AuthorId);

DECLARE @CompletePrimaryAuthorId INT =
    (SELECT TOP 1 am.AuthorId
     FROM dbo.AuthorMaster am
     LEFT JOIN dbo.RemedyRubricAuthorDetails rra
        ON rra.AuthorId = am.AuthorId AND ISNULL(rra.DeletedStatus, 0) = 0
     WHERE ISNULL(am.IsDeleted, 0) = 0
       AND (
            am.AuthorName LIKE N'%Complete%Repertory%'
         OR am.AuthorName LIKE N'%COMPLETE REPERTORY%'
         OR am.AuthorName LIKE N'%VAN ZANDVOORT%'
         OR am.AuthorName LIKE N'%ZANDVOORT%'
         OR am.AuthorAlias LIKE N'cr'
         OR am.AuthorAlias LIKE N'cr%'
         OR (am.AuthorName LIKE N'%Complete%' AND am.AuthorName NOT LIKE N'%Kent%')
       )
     GROUP BY am.AuthorId, am.AuthorName
     ORDER BY COUNT(rra.RubricRemedyId) DESC, am.AuthorId);

IF NOT EXISTS (SELECT 1 FROM dbo.RepertorySource WHERE SourceCode = N'KENT')
BEGIN
    INSERT INTO dbo.RepertorySource (SourceCode, SourceName, AuthorId, PriorityOrder, IsActive)
    VALUES (N'KENT', N'Kent Repertory', @KentPrimaryAuthorId, 1, 1);
END
ELSE
BEGIN
    UPDATE dbo.RepertorySource
    SET AuthorId = @KentPrimaryAuthorId
    WHERE SourceCode = N'KENT' AND (AuthorId IS NULL OR AuthorId <> @KentPrimaryAuthorId);
END

IF NOT EXISTS (SELECT 1 FROM dbo.RepertorySource WHERE SourceCode = N'COMPLETE')
BEGIN
    INSERT INTO dbo.RepertorySource (SourceCode, SourceName, AuthorId, PriorityOrder, IsActive)
    VALUES (N'COMPLETE', N'Complete Repertory', @CompletePrimaryAuthorId, 2, 1);
END
ELSE
BEGIN
    UPDATE dbo.RepertorySource
    SET AuthorId = @CompletePrimaryAuthorId
    WHERE SourceCode = N'COMPLETE' AND (AuthorId IS NULL OR AuthorId <> @CompletePrimaryAuthorId);
END
GO

DECLARE @KentSourceId BIGINT = (SELECT RepertorySourceId FROM dbo.RepertorySource WHERE SourceCode = N'KENT');
DECLARE @CompleteSourceId BIGINT = (SELECT RepertorySourceId FROM dbo.RepertorySource WHERE SourceCode = N'COMPLETE');

IF @KentSourceId IS NOT NULL
BEGIN
    INSERT INTO dbo.RubricRepertoryMap (SubSectionId, RepertorySourceId, SourceRubricKey, SourceRubricPath, MappingConfidence, IsPrimarySource, IsActive)
    SELECT DISTINCT
        rr.SubSectionId,
        @KentSourceId,
        ss.SubSectionName,
        sm.SectionName,
        1.0000,
        1,
        1
    FROM dbo.RubricRemedyDetails rr
    INNER JOIN dbo.SubSectionMaster ss ON ss.SubSectionId = rr.SubSectionId AND ISNULL(ss.DeleteStatus, 0) = 0
    LEFT JOIN dbo.SectionMaster sm ON sm.SectionId = ss.SectionId
    INNER JOIN dbo.RemedyRubricAuthorDetails rra ON rra.RubricRemedyId = rr.RubricRemedyId AND ISNULL(rra.DeletedStatus, 0) = 0
    INNER JOIN dbo.AuthorMaster am ON am.AuthorId = rra.AuthorId AND ISNULL(am.IsDeleted, 0) = 0
    WHERE ISNULL(rr.DeletedStatus, 0) = 0
      AND rr.SubSectionId IS NOT NULL
      AND (
            am.AuthorName LIKE N'%KENT%'
         OR am.AuthorAlias LIKE N'k'
         OR am.AuthorAlias LIKE N'k%'
      )
      AND am.AuthorName NOT LIKE N'%Complete%'
      AND am.AuthorName NOT LIKE N'%COMPAR%'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.RubricRepertoryMap m
          WHERE m.SubSectionId = rr.SubSectionId AND m.RepertorySourceId = @KentSourceId);
END

IF @CompleteSourceId IS NOT NULL
BEGIN
    INSERT INTO dbo.RubricRepertoryMap (SubSectionId, RepertorySourceId, SourceRubricKey, SourceRubricPath, MappingConfidence, IsPrimarySource, IsActive)
    SELECT DISTINCT
        rr.SubSectionId,
        @CompleteSourceId,
        ss.SubSectionName,
        sm.SectionName,
        0.9500,
        CASE WHEN NOT EXISTS (
            SELECT 1 FROM dbo.RubricRepertoryMap km
            WHERE km.SubSectionId = rr.SubSectionId AND km.RepertorySourceId = @KentSourceId AND km.IsActive = 1)
        THEN 1 ELSE 0 END,
        1
    FROM dbo.RubricRemedyDetails rr
    INNER JOIN dbo.SubSectionMaster ss ON ss.SubSectionId = rr.SubSectionId AND ISNULL(ss.DeleteStatus, 0) = 0
    LEFT JOIN dbo.SectionMaster sm ON sm.SectionId = ss.SectionId
    INNER JOIN dbo.RemedyRubricAuthorDetails rra ON rra.RubricRemedyId = rr.RubricRemedyId AND ISNULL(rra.DeletedStatus, 0) = 0
    INNER JOIN dbo.AuthorMaster am ON am.AuthorId = rra.AuthorId AND ISNULL(am.IsDeleted, 0) = 0
    WHERE ISNULL(rr.DeletedStatus, 0) = 0
      AND rr.SubSectionId IS NOT NULL
      AND (
            am.AuthorName LIKE N'%Complete%Repertory%'
         OR am.AuthorName LIKE N'%COMPLETE REPERTORY%'
         OR am.AuthorName LIKE N'%VAN ZANDVOORT%'
         OR am.AuthorName LIKE N'%ZANDVOORT%'
         OR am.AuthorAlias LIKE N'cr'
         OR am.AuthorAlias LIKE N'cr%'
         OR (am.AuthorName LIKE N'%Complete%' AND am.AuthorName NOT LIKE N'%Kent%')
      )
      AND NOT EXISTS (
          SELECT 1 FROM dbo.RubricRepertoryMap m
          WHERE m.SubSectionId = rr.SubSectionId AND m.RepertorySourceId = @CompleteSourceId);
END
GO

DECLARE @KentMapped INT = (
    SELECT COUNT(*) FROM dbo.RubricRepertoryMap m
    JOIN dbo.RepertorySource s ON s.RepertorySourceId = m.RepertorySourceId
    WHERE s.SourceCode = N'KENT' AND m.IsActive = 1);
DECLARE @CompleteMapped INT = (
    SELECT COUNT(*) FROM dbo.RubricRepertoryMap m
    JOIN dbo.RepertorySource s ON s.RepertorySourceId = m.RepertorySourceId
    WHERE s.SourceCode = N'COMPLETE' AND m.IsActive = 1);

PRINT CONCAT('Repertory seed complete. Kent maps: ', @KentMapped, ', Complete maps: ', @CompleteMapped);
GO
PRINT 'Phase 7 repertory deployment finished successfully.';
GO

GO

/* ==============================================================================
   SECTION: V3 Concept Graph
   ============================================================================== */
PRINT '';
PRINT '>>> V3 Concept Graph';
GO

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

GO

/* ==============================================================================
   SECTION: V3.5 Recall Engine
   ============================================================================== */
PRINT '';
PRINT '>>> V3.5 Recall Engine';
GO

-- =============================================================================
-- HomeoCentrum AI Rubric Intelligence V3.5 — RECALL OPTIMIZATION DEPLOY
-- Run on database with V3 already deployed (000_DEPLOY_V3_ALL.sql).
-- Safe to re-run (idempotent). Does NOT modify SectionMaster/SubSectionMaster.
-- =============================================================================

SET NOCOUNT ON;
PRINT '=== V3.5 RECALL ENGINE DEPLOY START ===';
GO

/* ---------- SESSION SCORES ---------- */

IF COL_LENGTH('dbo.AudioCaseSession', 'TranscriptCoverageScore') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession ADD TranscriptCoverageScore DECIMAL(5,4) NULL;
    PRINT 'Added AudioCaseSession.TranscriptCoverageScore';
END
GO

IF COL_LENGTH('dbo.AudioCaseSession', 'CaseCompletenessScore') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession ADD CaseCompletenessScore DECIMAL(5,4) NULL;
    PRINT 'Added AudioCaseSession.CaseCompletenessScore';
END
GO

IF COL_LENGTH('dbo.AudioCaseSession', 'RecallEngineVersion') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession ADD RecallEngineVersion NVARCHAR(10) NULL;
    PRINT 'Added AudioCaseSession.RecallEngineVersion';
END
GO

/* ---------- V3.5 TABLES ---------- */

IF OBJECT_ID(N'dbo.AISymptomBlock', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AISymptomBlock
    (
        SymptomBlockId       BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        BlockOrder           INT NOT NULL CONSTRAINT DF_AISymptomBlock_Order DEFAULT (0),
        TranscriptSpan       NVARCHAR(2000) NOT NULL,
        CategoryHint         NVARCHAR(50) NOT NULL,
        BlockType            NVARCHAR(50) NULL,
        Confidence           DECIMAL(5,4) NOT NULL,
        IsCovered            BIT NOT NULL CONSTRAINT DF_AISymptomBlock_Covered DEFAULT (0),
        ModelVersion         NVARCHAR(20) NOT NULL CONSTRAINT DF_AISymptomBlock_Model DEFAULT (N'v3.5-m0'),
        EnteredDate          DATETIME2 NOT NULL CONSTRAINT DF_AISymptomBlock_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AISymptomBlock PRIMARY KEY (SymptomBlockId),
        CONSTRAINT FK_AISymptomBlock_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AISymptomBlock';
END
GO

IF COL_LENGTH('dbo.AIPatientMeaning', 'ParentMeaningId') IS NULL
BEGIN
    ALTER TABLE dbo.AIPatientMeaning ADD ParentMeaningId BIGINT NULL;
    PRINT 'Added AIPatientMeaning.ParentMeaningId';
END
GO

IF COL_LENGTH('dbo.AIPatientMeaning', 'SymptomBlockId') IS NULL
BEGIN
    ALTER TABLE dbo.AIPatientMeaning ADD SymptomBlockId BIGINT NULL;
    PRINT 'Added AIPatientMeaning.SymptomBlockId';
END
GO

IF COL_LENGTH('dbo.AIPatientMeaning', 'SymptomCategory') IS NULL
BEGIN
    ALTER TABLE dbo.AIPatientMeaning ADD SymptomCategory NVARCHAR(50) NULL;
    PRINT 'Added AIPatientMeaning.SymptomCategory';
END
GO

IF COL_LENGTH('dbo.AIClinicalConcept', 'SymptomCategory') IS NULL
BEGIN
    ALTER TABLE dbo.AIClinicalConcept ADD SymptomCategory NVARCHAR(50) NULL;
    PRINT 'Added AIClinicalConcept.SymptomCategory';
END
GO

IF COL_LENGTH('dbo.AIHomeopathicConcept', 'ClusterId') IS NULL
BEGIN
    ALTER TABLE dbo.AIHomeopathicConcept ADD ClusterId BIGINT NULL;
    PRINT 'Added AIHomeopathicConcept.ClusterId';
END
GO

IF COL_LENGTH('dbo.AIRubricDiscovery', 'RubricTier') IS NULL
BEGIN
    ALTER TABLE dbo.AIRubricDiscovery ADD RubricTier NVARCHAR(20) NULL;
    PRINT 'Added AIRubricDiscovery.RubricTier';
END
GO

IF OBJECT_ID(N'dbo.AIConceptCluster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIConceptCluster
    (
        ConceptClusterId       BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId     UNIQUEIDENTIFIER NOT NULL,
        ClusterLabel           NVARCHAR(200) NOT NULL,
        SymptomBlockId         BIGINT NULL,
        MemberCount            INT NOT NULL CONSTRAINT DF_AIConceptCluster_Members DEFAULT (0),
        EnteredDate            DATETIME2 NOT NULL CONSTRAINT DF_AIConceptCluster_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIConceptCluster PRIMARY KEY (ConceptClusterId),
        CONSTRAINT FK_AIConceptCluster_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIConceptCluster';
END
GO

IF OBJECT_ID(N'dbo.AIConceptClusterMember', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIConceptClusterMember
    (
        ConceptClusterMemberId BIGINT IDENTITY(1,1) NOT NULL,
        ConceptClusterId       BIGINT NOT NULL,
        HomeopathicConceptId   BIGINT NOT NULL,
        RankOrder              INT NOT NULL CONSTRAINT DF_AIConceptClusterMember_Rank DEFAULT (1),
        EnteredDate            DATETIME2 NOT NULL CONSTRAINT DF_AIConceptClusterMember_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIConceptClusterMember PRIMARY KEY (ConceptClusterMemberId),
        CONSTRAINT FK_AIConceptClusterMember_Cluster FOREIGN KEY (ConceptClusterId)
            REFERENCES dbo.AIConceptCluster (ConceptClusterId),
        CONSTRAINT FK_AIConceptClusterMember_Homeopathic FOREIGN KEY (HomeopathicConceptId)
            REFERENCES dbo.AIHomeopathicConcept (HomeopathicConceptId)
    );
    PRINT 'Created AIConceptClusterMember';
END
GO

IF OBJECT_ID(N'dbo.AICaseCoverageMetrics', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AICaseCoverageMetrics
    (
        CaseCoverageMetricsId  BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId     UNIQUEIDENTIFIER NOT NULL,
        TranscriptCoverage     DECIMAL(5,4) NOT NULL,
        CaseCompleteness       DECIMAL(5,4) NOT NULL,
        TotalBlocks            INT NOT NULL,
        CoveredBlocks          INT NOT NULL,
        Tier1Count             INT NOT NULL CONSTRAINT DF_AICaseCoverage_T1 DEFAULT (0),
        Tier2Count             INT NOT NULL CONSTRAINT DF_AICaseCoverage_T2 DEFAULT (0),
        Tier3Count             INT NOT NULL CONSTRAINT DF_AICaseCoverage_T3 DEFAULT (0),
        MissingSymptomCount    INT NOT NULL CONSTRAINT DF_AICaseCoverage_Missing DEFAULT (0),
        MetricsJson            NVARCHAR(MAX) NULL,
        EnteredDate            DATETIME2 NOT NULL CONSTRAINT DF_AICaseCoverage_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AICaseCoverageMetrics PRIMARY KEY (CaseCoverageMetricsId),
        CONSTRAINT FK_AICaseCoverageMetrics_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AICaseCoverageMetrics';
END
GO

IF OBJECT_ID(N'dbo.AIMissingSymptomCandidate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIMissingSymptomCandidate
    (
        MissingSymptomCandidateId BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId        UNIQUEIDENTIFIER NOT NULL,
        SymptomBlockId            BIGINT NULL,
        TranscriptSpan            NVARCHAR(2000) NOT NULL,
        CategoryHint              NVARCHAR(50) NOT NULL,
        ResolutionStatus          NVARCHAR(20) NOT NULL CONSTRAINT DF_AIMissingSymptom_Status DEFAULT (N'Pending'),
        ResolvedRubricCount       INT NOT NULL CONSTRAINT DF_AIMissingSymptom_Resolved DEFAULT (0),
        EnteredDate               DATETIME2 NOT NULL CONSTRAINT DF_AIMissingSymptom_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIMissingSymptomCandidate PRIMARY KEY (MissingSymptomCandidateId),
        CONSTRAINT FK_AIMissingSymptomCandidate_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
    PRINT 'Created AIMissingSymptomCandidate';
END
GO

/* ---------- INDEXES ---------- */

IF OBJECT_ID(N'dbo.AISymptomBlock', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AISymptomBlock_Session' AND object_id = OBJECT_ID(N'dbo.AISymptomBlock'))
    CREATE INDEX IX_AISymptomBlock_Session ON dbo.AISymptomBlock (AudioCaseSessionId, BlockOrder);
GO

IF OBJECT_ID(N'dbo.AIConceptCluster', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIConceptCluster_Session' AND object_id = OBJECT_ID(N'dbo.AIConceptCluster'))
    CREATE INDEX IX_AIConceptCluster_Session ON dbo.AIConceptCluster (AudioCaseSessionId);
GO

IF OBJECT_ID(N'dbo.AICaseCoverageMetrics', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AICaseCoverageMetrics_Session' AND object_id = OBJECT_ID(N'dbo.AICaseCoverageMetrics'))
    CREATE INDEX IX_AICaseCoverageMetrics_Session ON dbo.AICaseCoverageMetrics (AudioCaseSessionId, EnteredDate DESC);
GO

IF OBJECT_ID(N'dbo.AIMissingSymptomCandidate', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIMissingSymptomCandidate_Session' AND object_id = OBJECT_ID(N'dbo.AIMissingSymptomCandidate'))
    CREATE INDEX IX_AIMissingSymptomCandidate_Session ON dbo.AIMissingSymptomCandidate (AudioCaseSessionId, ResolutionStatus);
GO

/* ---------- BOOTSTRAP: EPILEPSY FULL CASE + RECALL PATTERNS (706) ---------- */

IF OBJECT_ID(N'dbo.AIConceptMappingBootstrap', N'U') IS NOT NULL
BEGIN
    ;WITH Seed706 AS (
        SELECT * FROM (VALUES
            (N'Fear Before Convulsion',       N'%FEAR%CONVULS%BEFORE%',      N'Mind',        1),
            (N'Fear Of Convulsions',            N'%FEAR%CONVULS%',             N'Mind',        1),
            (N'Anticipatory Anxiety',           N'%FEAR%CONVULS%',             N'Mind',        2),
            (N'Epileptic Anticipation',         N'%FEAR%FIT%',                 N'Neurology',   1),
            (N'Forgetfulness',                  N'%FORGET%',                   N'Mind',        1),
            (N'Forgetfulness',                  N'%MEMORY%WEAK%',              N'Mind',        2),
            (N'Fear Of Heights',                N'%FEAR%HEIGHT%',              N'Mind',        1),
            (N'Fear Of High Places',            N'%FEAR%HIGH%PLACE%',          N'Mind',        1),
            (N'Anger',                          N'%ANGER%',                    N'Mind',        1),
            (N'Ailments From Anger',            N'%AILMENT%ANGER%',            N'Mind',        1),
            (N'Ailments From Anger',            N'%ANGER%AILMENT%',            N'Mind',        2),
            (N'Dropping Things',                N'%DROP%THING%',               N'Extremities', 1),
            (N'Loss of Grip',                   N'%DROP%HAND%',                N'Extremities', 1),
            (N'Desire For Mutton',              N'%MUTTON%DESIRE%',            N'Food',        1),
            (N'Desire For Mutton',              N'%DESIRE%MUTTON%',            N'Food',        2),
            (N'Aversion To Sweets',             N'%SWEET%AVERSION%',           N'Food',        1),
            (N'Aversion To Sweets',             N'%AVERSION%SWEET%',           N'Food',        2),
            (N'Sleep Talking',                  N'%SLEEP%TALK%',               N'Sleep',       1),
            (N'Somnambulism',                   N'%SLEEP%WALK%',               N'Sleep',       1),
            (N'Sexual Dreams',                  N'%DREAM%SEXUAL%',             N'Dreams',      1),
            (N'Sexual Desire Increased',        N'%SEXUAL%DESIRE%INCREAS%',    N'Sexual',      1),
            (N'Sexual Desire Increased',        N'%DESIRE%SEXUAL%',            N'Sexual',      2),
            (N'Shock Sensation',                N'%SHOCK%SENSAT%',             N'General',     1),
            (N'Vibration Sensation',          N'%VIBRAT%',                   N'General',     1),
            (N'Convulsion Aura',                N'%CONVULS%AURA%',             N'Neurology',   1)
        ) AS v(HomeopathicConceptPattern, SubSectionNamePattern, Domain, PriorityOrder)
    )
    INSERT INTO dbo.AIConceptMappingBootstrap
        (HomeopathicConceptPattern, SubSectionNamePattern, Domain, PriorityOrder, IsActive)
    SELECT s.HomeopathicConceptPattern, s.SubSectionNamePattern, s.Domain, s.PriorityOrder, 1
    FROM Seed706 s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.AIConceptMappingBootstrap b
        WHERE b.HomeopathicConceptPattern = s.HomeopathicConceptPattern
          AND b.SubSectionNamePattern = s.SubSectionNamePattern
    );
    PRINT 'Seeded 706 epilepsy + recall bootstrap patterns';
END
GO

PRINT '=== V3.5 RECALL ENGINE DEPLOY COMPLETE ===';
PRINT 'Next: restart API with EnableV35RecallEngine=true in appsettings.json';
GO

GO

/* ==============================================================================
   SECTION: V2.1 Clinical Validation
   ============================================================================== */
PRINT '';
PRINT '>>> V2.1 Clinical Validation';
GO

-- Phase 12 / V2.1: Clinical validation rules and logging
IF OBJECT_ID(N'dbo.RubricGenderRule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RubricGenderRule
    (
        RubricGenderRuleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TokenPattern NVARCHAR(100) NOT NULL,
        AllowedGender TINYINT NOT NULL, -- 0=male, 1=female
        IsActive BIT NOT NULL CONSTRAINT DF_RubricGenderRule_IsActive DEFAULT (1),
        EnteredDate DATETIME2 NOT NULL CONSTRAINT DF_RubricGenderRule_EnteredDate DEFAULT (SYSUTCDATETIME())
    );
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
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'EvidenceChainJson') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD EvidenceChainJson NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'QualityScore') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD QualityScore DECIMAL(5,2) NULL;
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'ValidationStatus') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD ValidationStatus NVARCHAR(20) NULL;
END
GO

-- Seed female-only tokens (runtime also enforces in code)
IF NOT EXISTS (SELECT 1 FROM dbo.RubricGenderRule WHERE TokenPattern = 'MENSES')
BEGIN
    INSERT INTO dbo.RubricGenderRule (TokenPattern, AllowedGender) VALUES
    ('MENSES', 1), ('MENSTRU', 1), ('PREGNAN', 1), ('LABOR', 1), ('OVAR', 1),
    ('UTER', 1), ('VAGIN', 1), ('LOCHIA', 1), ('MISCARRI', 1), ('MENOPAUSE', 1),
    ('PROSTATE', 0), ('TESTES', 0), ('TESTIC', 0), ('SCROTUM', 0);
END
GO

PRINT '601_Create_RubricValidationRules completed.';
GO

GO

/* ==============================================================================
   SECTION: V4 Embedding Infrastructure
   ============================================================================== */
PRINT '';
PRINT '>>> V4 Embedding Infrastructure';
GO

/*
  HOMEOCENTRUM AI V4 — Phase 1: Enterprise Embedding Infrastructure
  Creates NEW tables only. Does NOT alter repertory master tables.
  Legacy dbo.RubricEmbeddings remains untouched for backward compatibility.
*/
SET NOCOUNT ON;
GO

/* ---------- AIEmbeddingVersion ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingVersion' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingVersion
    (
        EmbeddingVersionId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_AIEmbeddingVersion_Id DEFAULT (NEWSEQUENTIALID()),
        VersionCode          NVARCHAR(50)     NOT NULL,
        ModelProvider        NVARCHAR(50)     NOT NULL,
        ModelName            NVARCHAR(100)    NOT NULL,
        ModelVersion         NVARCHAR(50)     NULL,
        DimensionCount       INT              NOT NULL,
        VectorFormat         NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIEmbeddingVersion_VectorFormat DEFAULT (N'JsonFloatArray'),
        IsActive             BIT              NOT NULL CONSTRAINT DF_AIEmbeddingVersion_IsActive DEFAULT (1),
        IsCurrent            BIT              NOT NULL CONSTRAINT DF_AIEmbeddingVersion_IsCurrent DEFAULT (0),
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIEmbeddingVersion_Status DEFAULT (N'Draft'),
        Description          NVARCHAR(500)    NULL,
        ConfigurationJson    NVARCHAR(MAX)    NULL,
        CreatedByUserId      INT              NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingVersion_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIEmbeddingVersion_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIEmbeddingVersion PRIMARY KEY CLUSTERED (EmbeddingVersionId),
        CONSTRAINT UQ_AIEmbeddingVersion_VersionCode UNIQUE (VersionCode)
    );
END
GO

/* ---------- AIRubricEmbedding ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIRubricEmbedding' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIRubricEmbedding
    (
        RubricEmbeddingId    BIGINT           IDENTITY(1,1) NOT NULL,
        EmbeddingVersionId   UNIQUEIDENTIFIER NOT NULL,
        RubricId             INT              NOT NULL,
        SourceText           NVARCHAR(2000)   NOT NULL,
        TextHash             CHAR(64)         NOT NULL,
        EmbeddingPayloadJson NVARCHAR(MAX)    NULL,
        DimensionCount       INT              NOT NULL,
        RevisionNo           INT              NOT NULL CONSTRAINT DF_AIRubricEmbedding_RevisionNo DEFAULT (1),
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIRubricEmbedding_Status DEFAULT (N'Pending'),
        SourceType           NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIRubricEmbedding_SourceType DEFAULT (N'SubSectionName'),
        LastJobId            UNIQUEIDENTIFIER NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIRubricEmbedding_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIRubricEmbedding_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIRubricEmbedding PRIMARY KEY CLUSTERED (RubricEmbeddingId),
        CONSTRAINT FK_AIRubricEmbedding_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId),
        CONSTRAINT FK_AIRubricEmbedding_SubSection FOREIGN KEY (RubricId)
            REFERENCES dbo.SubSectionMaster (SubSectionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIRubricEmbedding_Version_Rubric_Active' AND object_id = OBJECT_ID(N'dbo.AIRubricEmbedding'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_AIRubricEmbedding_Version_Rubric_Active
        ON dbo.AIRubricEmbedding (EmbeddingVersionId, RubricId)
        WHERE IsDeleted = 0 AND Status IN (N'Pending', N'Active');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIRubricEmbedding_TextHash' AND object_id = OBJECT_ID(N'dbo.AIRubricEmbedding'))
    CREATE NONCLUSTERED INDEX IX_AIRubricEmbedding_TextHash ON dbo.AIRubricEmbedding (TextHash, EmbeddingVersionId);
GO

/* ---------- AIConceptEmbedding ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIConceptEmbedding' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIConceptEmbedding
    (
        ConceptEmbeddingId   BIGINT           IDENTITY(1,1) NOT NULL,
        EmbeddingVersionId   UNIQUEIDENTIFIER NOT NULL,
        ConceptKey           NVARCHAR(200)    NOT NULL,
        ConceptType          NVARCHAR(50)     NOT NULL,
        SourceDomain         NVARCHAR(50)     NULL,
        SourceText             NVARCHAR(2000)   NOT NULL,
        TextHash             CHAR(64)         NOT NULL,
        EmbeddingPayloadJson NVARCHAR(MAX)    NULL,
        DimensionCount       INT              NOT NULL,
        RevisionNo           INT              NOT NULL CONSTRAINT DF_AIConceptEmbedding_RevisionNo DEFAULT (1),
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIConceptEmbedding_Status DEFAULT (N'Pending'),
        LastJobId            UNIQUEIDENTIFIER NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIConceptEmbedding_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIConceptEmbedding_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIConceptEmbedding PRIMARY KEY CLUSTERED (ConceptEmbeddingId),
        CONSTRAINT FK_AIConceptEmbedding_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIConceptEmbedding_Version_Key_Type_Active' AND object_id = OBJECT_ID(N'dbo.AIConceptEmbedding'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_AIConceptEmbedding_Version_Key_Type_Active
        ON dbo.AIConceptEmbedding (EmbeddingVersionId, ConceptKey, ConceptType)
        WHERE IsDeleted = 0 AND Status IN (N'Pending', N'Active');
GO

/* ---------- AIEmbeddingJob ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingJob' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingJob
    (
        JobId                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_AIEmbeddingJob_Id DEFAULT (NEWID()),
        EmbeddingVersionId   UNIQUEIDENTIFIER NOT NULL,
        JobType              NVARCHAR(50)     NOT NULL,
        TriggerSource        NVARCHAR(50)     NULL,
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIEmbeddingJob_Status DEFAULT (N'Queued'),
        Priority             INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_Priority DEFAULT (100),
        TotalItems           INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_TotalItems DEFAULT (0),
        ProcessedItems       INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_ProcessedItems DEFAULT (0),
        FailedItems          INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_FailedItems DEFAULT (0),
        SkippedItems         INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_SkippedItems DEFAULT (0),
        RetryCount           INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_RetryCount DEFAULT (0),
        MaxRetries           INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_MaxRetries DEFAULT (3),
        CorrelationId        NVARCHAR(64)     NULL,
        ErrorSummary         NVARCHAR(2000)   NULL,
        StartedAtUtc         DATETIME2(3)     NULL,
        CompletedAtUtc       DATETIME2(3)     NULL,
        CreatedByUserId      INT              NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingJob_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIEmbeddingJob PRIMARY KEY CLUSTERED (JobId),
        CONSTRAINT FK_AIEmbeddingJob_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingJob_Status_Priority' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingJob'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingJob_Status_Priority
        ON dbo.AIEmbeddingJob (Status, Priority, CreatedDate)
        WHERE IsDeleted = 0;
GO

/* ---------- AIEmbeddingQueue ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingQueue' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingQueue
    (
        QueueId              BIGINT           IDENTITY(1,1) NOT NULL,
        JobId                UNIQUEIDENTIFIER NOT NULL,
        ItemType             NVARCHAR(30)     NOT NULL,
        RubricId             INT              NULL,
        ConceptKey           NVARCHAR(200)    NULL,
        ConceptType          NVARCHAR(50)     NULL,
        PayloadJson          NVARCHAR(MAX)    NULL,
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIEmbeddingQueue_Status DEFAULT (N'Pending'),
        Priority             INT              NOT NULL CONSTRAINT DF_AIEmbeddingQueue_Priority DEFAULT (100),
        AttemptCount         INT              NOT NULL CONSTRAINT DF_AIEmbeddingQueue_AttemptCount DEFAULT (0),
        MaxAttempts          INT              NOT NULL CONSTRAINT DF_AIEmbeddingQueue_MaxAttempts DEFAULT (3),
        NextRetryAtUtc       DATETIME2(3)     NULL,
        LockedUntilUtc       DATETIME2(3)     NULL,
        LockedBy             NVARCHAR(100)    NULL,
        LastError            NVARCHAR(2000)   NULL,
        CompletedAtUtc       DATETIME2(3)     NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingQueue_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIEmbeddingQueue_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIEmbeddingQueue PRIMARY KEY CLUSTERED (QueueId),
        CONSTRAINT FK_AIEmbeddingQueue_Job FOREIGN KEY (JobId)
            REFERENCES dbo.AIEmbeddingJob (JobId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingQueue_Pending' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingQueue'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingQueue_Pending
        ON dbo.AIEmbeddingQueue (Status, Priority, NextRetryAtUtc, CreatedDate)
        WHERE IsDeleted = 0 AND Status IN (N'Pending', N'Processing');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingQueue_JobId' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingQueue'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingQueue_JobId ON dbo.AIEmbeddingQueue (JobId, Status);
GO

/* ---------- AIEmbeddingAudit ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingAudit' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingAudit
    (
        AuditId              BIGINT           IDENTITY(1,1) NOT NULL,
        EntityType           NVARCHAR(50)     NOT NULL,
        EntityId             NVARCHAR(100)    NOT NULL,
        Action               NVARCHAR(50)     NOT NULL,
        OldStatus            NVARCHAR(30)     NULL,
        NewStatus            NVARCHAR(30)     NULL,
        JobId                UNIQUEIDENTIFIER NULL,
        QueueId              BIGINT           NULL,
        EmbeddingVersionId   UNIQUEIDENTIFIER NULL,
        ActorUserId          INT              NULL,
        CorrelationId        NVARCHAR(64)     NULL,
        DetailsJson          NVARCHAR(MAX)    NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingAudit_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIEmbeddingAudit PRIMARY KEY CLUSTERED (AuditId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingAudit_Entity' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingAudit'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingAudit_Entity
        ON dbo.AIEmbeddingAudit (EntityType, EntityId, CreatedDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingAudit_Job' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingAudit'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingAudit_Job ON dbo.AIEmbeddingAudit (JobId, CreatedDate DESC);
GO

/* ---------- AIEmbeddingStatistics ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingStatistics' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingStatistics
    (
        StatId                   BIGINT           IDENTITY(1,1) NOT NULL,
        EmbeddingVersionId       UNIQUEIDENTIFIER NOT NULL,
        StatDate                 DATE             NOT NULL,
        RubricEmbeddingCount     INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_RubricCount DEFAULT (0),
        ConceptEmbeddingCount    INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_ConceptCount DEFAULT (0),
        ActiveRubricEmbeddings   INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_ActiveRubric DEFAULT (0),
        ActiveConceptEmbeddings  INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_ActiveConcept DEFAULT (0),
        QueuePendingCount        INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_QueuePending DEFAULT (0),
        QueueProcessingCount     INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_QueueProcessing DEFAULT (0),
        QueueFailedCount         INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_QueueFailed DEFAULT (0),
        QueueDeadLetterCount     INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_QueueDeadLetter DEFAULT (0),
        JobsCompleted            INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_JobsCompleted DEFAULT (0),
        JobsFailed               INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_JobsFailed DEFAULT (0),
        AvgProcessingMs          INT              NULL,
        CreatedDate              DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate              DATETIME2(3)     NULL,
        CONSTRAINT PK_AIEmbeddingStatistics PRIMARY KEY CLUSTERED (StatId),
        CONSTRAINT FK_AIEmbeddingStatistics_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId),
        CONSTRAINT UQ_AIEmbeddingStatistics_Version_Date UNIQUE (EmbeddingVersionId, StatDate)
    );
END
GO

PRINT 'AI V4 Phase 1 embedding infrastructure tables created (or already exist).';
GO

GO

/* ==============================================================================
   SECTION: V4 Incremental Sync
   ============================================================================== */
PRINT '';
PRINT '>>> V4 Incremental Sync';
GO

/*
  HOMEOCENTRUM AI V4 — Phase 3: Incremental Embedding Refresh
  Adds sync watermark table. Does NOT alter repertory master tables.
*/
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingSyncState' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingSyncState
    (
        SyncStateId              BIGINT           IDENTITY(1,1) NOT NULL,
        SyncScope                NVARCHAR(50)     NOT NULL,
        EmbeddingVersionId       UNIQUEIDENTIFIER NULL,
        LastSuccessfulSyncUtc    DATETIME2(3)     NULL,
        LastScanStartedUtc       DATETIME2(3)     NULL,
        LastScanCompletedUtc     DATETIME2(3)     NULL,
        LastDetectedCount        INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastDetectedCount DEFAULT (0),
        LastEnqueuedCount        INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastEnqueuedCount DEFAULT (0),
        LastProcessedCount       INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastProcessedCount DEFAULT (0),
        LastSkippedCount         INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastSkippedCount DEFAULT (0),
        LastFailedCount          INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastFailedCount DEFAULT (0),
        LastRunCorrelationId     NVARCHAR(64)     NULL,
        LastJobId                UNIQUEIDENTIFIER NULL,
        LastError                NVARCHAR(2000)   NULL,
        CreatedDate              DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate              DATETIME2(3)     NULL,
        CONSTRAINT PK_AIEmbeddingSyncState PRIMARY KEY CLUSTERED (SyncStateId),
        CONSTRAINT FK_AIEmbeddingSyncState_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AIEmbeddingSyncState_Scope_Version' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingSyncState'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AIEmbeddingSyncState_Scope_Version
        ON dbo.AIEmbeddingSyncState (SyncScope, EmbeddingVersionId)
        WHERE EmbeddingVersionId IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AIEmbeddingSyncState_Scope_Global' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingSyncState'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AIEmbeddingSyncState_Scope_Global
        ON dbo.AIEmbeddingSyncState (SyncScope)
        WHERE EmbeddingVersionId IS NULL;
GO

GO

/* ==============================================================================
   SECTION: Phase 10 Monitoring
   ============================================================================== */
PRINT '';
PRINT '>>> Phase 10 Monitoring';
GO

-- Phase 10: AI Monitoring Dashboard — daily KPI snapshots and enterprise audit log
-- Does NOT modify repertory master tables.

IF OBJECT_ID(N'dbo.AIMonitoringDailySnapshot', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIMonitoringDailySnapshot
    (
        SnapshotId                  BIGINT IDENTITY(1,1) NOT NULL,
        SnapshotDate                DATE NOT NULL,
        EngineVersion               NVARCHAR(20) NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Engine DEFAULT (N'all'),
        SessionsAnalyzed            INT NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Sessions DEFAULT (0),
        FeedbackCount               INT NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Feedback DEFAULT (0),
        PrecisionScore              DECIMAL(5,4) NULL,
        RecallScore                 DECIMAL(5,4) NULL,
        DoctorAcceptanceRate          DECIMAL(5,4) NULL,
        HallucinationCount            INT NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Hallucinations DEFAULT (0),
        HallucinationRate             DECIMAL(5,4) NULL,
        EmbeddingFreshnessHours       DECIMAL(10,2) NULL,
        RubricEmbeddingCoverage       DECIMAL(5,4) NULL,
        ConceptEmbeddingCoverage      DECIMAL(5,4) NULL,
        TranscriptCoverage            DECIMAL(5,4) NULL,
        AverageConfidence             DECIMAL(5,4) NULL,
        PrimaryRubricAccuracy         DECIMAL(5,4) NULL,
        F1Score                       DECIMAL(5,4) NULL,
        MetricsJson                   NVARCHAR(MAX) NULL,
        CalculatedDate                DATETIME2 NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Calculated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIMonitoringDailySnapshot PRIMARY KEY (SnapshotId),
        CONSTRAINT UQ_AIMonitoringDailySnapshot_DateEngine UNIQUE (SnapshotDate, EngineVersion)
    );
    PRINT 'Created AIMonitoringDailySnapshot';
END
GO

IF OBJECT_ID(N'dbo.AIMonitoringDailySnapshot', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIMonitoringDailySnapshot_Date' AND object_id = OBJECT_ID(N'dbo.AIMonitoringDailySnapshot'))
BEGIN
    CREATE INDEX IX_AIMonitoringDailySnapshot_Date
        ON dbo.AIMonitoringDailySnapshot (SnapshotDate DESC, EngineVersion);
END
GO

IF OBJECT_ID(N'dbo.AIMonitoringAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIMonitoringAuditLog
    (
        AuditLogId          BIGINT IDENTITY(1,1) NOT NULL,
        EventType           NVARCHAR(50) NOT NULL,
        Operation           NVARCHAR(100) NOT NULL,
        ActorUserId         INT NULL,
        CorrelationId       UNIQUEIDENTIFIER NULL,
        DetailsJson         NVARCHAR(MAX) NULL,
        DurationMs          INT NULL,
        Success             BIT NOT NULL CONSTRAINT DF_AIMonitoringAuditLog_Success DEFAULT (1),
        ErrorMessage        NVARCHAR(2000) NULL,
        EnteredDate         DATETIME2 NOT NULL CONSTRAINT DF_AIMonitoringAuditLog_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIMonitoringAuditLog PRIMARY KEY (AuditLogId)
    );
    PRINT 'Created AIMonitoringAuditLog';
END
GO

IF OBJECT_ID(N'dbo.AIMonitoringAuditLog', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIMonitoringAuditLog_EnteredDate' AND object_id = OBJECT_ID(N'dbo.AIMonitoringAuditLog'))
BEGIN
    CREATE INDEX IX_AIMonitoringAuditLog_EnteredDate
        ON dbo.AIMonitoringAuditLog (EnteredDate DESC, EventType);
END
GO

GO

/* ==============================================================================
   SECTION: Phase 11 Knowledge Graph
   ============================================================================== */
PRINT '';
PRINT '>>> Phase 11 Knowledge Graph';
GO

-- Phase 11: Enterprise Homeopathic Knowledge Graph (AI tables only — no repertory master mutation)

IF OBJECT_ID(N'dbo.AIKGNode', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIKGNode
    (
        NodeId              BIGINT IDENTITY(1,1) NOT NULL,
        NodeType            NVARCHAR(40) NOT NULL,
        CanonicalKey        NVARCHAR(500) NOT NULL,
        DisplayText         NVARCHAR(2000) NOT NULL,
        LanguageCode        NVARCHAR(10) NULL,
        ExpressionKind      NVARCHAR(30) NULL,
        SubSectionId        INT NULL,
        RemedyId            INT NULL,
        GradeId             INT NULL,
        Domain              NVARCHAR(50) NULL,
        SymptomCategory     NVARCHAR(50) NULL,
        Confidence          DECIMAL(5,4) NOT NULL CONSTRAINT DF_AIKGNode_Confidence DEFAULT (0.7000),
        Status              NVARCHAR(20) NOT NULL CONSTRAINT DF_AIKGNode_Status DEFAULT (N'Active'),
        MetadataJson        NVARCHAR(MAX) NULL,
        EnteredDate         DATETIME2 NOT NULL CONSTRAINT DF_AIKGNode_Entered DEFAULT (SYSUTCDATETIME()),
        UpdatedDate         DATETIME2 NULL,
        CONSTRAINT PK_AIKGNode PRIMARY KEY (NodeId)
    );
    PRINT 'Created AIKGNode';
END
GO

IF OBJECT_ID(N'dbo.AIKGNode', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIKGNode_TypeKey' AND object_id = OBJECT_ID(N'dbo.AIKGNode'))
    CREATE UNIQUE INDEX IX_AIKGNode_TypeKey ON dbo.AIKGNode (NodeType, CanonicalKey, LanguageCode);
GO

IF OBJECT_ID(N'dbo.AIKGNode', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIKGNode_SubSection' AND object_id = OBJECT_ID(N'dbo.AIKGNode'))
    CREATE INDEX IX_AIKGNode_SubSection ON dbo.AIKGNode (SubSectionId) WHERE SubSectionId IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.AIKGEdge', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIKGEdge
    (
        EdgeId              BIGINT IDENTITY(1,1) NOT NULL,
        FromNodeId          BIGINT NOT NULL,
        ToNodeId            BIGINT NOT NULL,
        EdgeType            NVARCHAR(40) NOT NULL,
        Weight              DECIMAL(6,3) NOT NULL CONSTRAINT DF_AIKGEdge_Weight DEFAULT (1.000),
        Confidence          DECIMAL(5,4) NOT NULL CONSTRAINT DF_AIKGEdge_Confidence DEFAULT (0.7000),
        IsProvisional       BIT NOT NULL CONSTRAINT DF_AIKGEdge_Provisional DEFAULT (0),
        Source              NVARCHAR(50) NOT NULL CONSTRAINT DF_AIKGEdge_Source DEFAULT (N'Bootstrap'),
        SourceSessionId     UNIQUEIDENTIFIER NULL,
        EnteredDate         DATETIME2 NOT NULL CONSTRAINT DF_AIKGEdge_Entered DEFAULT (SYSUTCDATETIME()),
        UpdatedDate         DATETIME2 NULL,
        CONSTRAINT PK_AIKGEdge PRIMARY KEY (EdgeId),
        CONSTRAINT FK_AIKGEdge_From FOREIGN KEY (FromNodeId) REFERENCES dbo.AIKGNode (NodeId),
        CONSTRAINT FK_AIKGEdge_To FOREIGN KEY (ToNodeId) REFERENCES dbo.AIKGNode (NodeId)
    );
    PRINT 'Created AIKGEdge';
END
GO

IF OBJECT_ID(N'dbo.AIKGEdge', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIKGEdge_FromType' AND object_id = OBJECT_ID(N'dbo.AIKGEdge'))
    CREATE INDEX IX_AIKGEdge_FromType ON dbo.AIKGEdge (FromNodeId, EdgeType) INCLUDE (ToNodeId, Weight, Confidence, IsProvisional);
GO

IF OBJECT_ID(N'dbo.AIKGEdgeEvidence', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIKGEdgeEvidence
    (
        EdgeEvidenceId      BIGINT IDENTITY(1,1) NOT NULL,
        EdgeId              BIGINT NOT NULL,
        AudioCaseSessionId  UNIQUEIDENTIFIER NULL,
        TranscriptSpan      NVARCHAR(2000) NULL,
        FeedbackId          BIGINT NULL,
        EmbeddingScore      DECIMAL(5,4) NULL,
        EnteredDate         DATETIME2 NOT NULL CONSTRAINT DF_AIKGEdgeEvidence_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIKGEdgeEvidence PRIMARY KEY (EdgeEvidenceId),
        CONSTRAINT FK_AIKGEdgeEvidence_Edge FOREIGN KEY (EdgeId) REFERENCES dbo.AIKGEdge (EdgeId)
    );
    PRINT 'Created AIKGEdgeEvidence';
END
GO

IF OBJECT_ID(N'dbo.AIKGFeedbackMutation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIKGFeedbackMutation
    (
        FeedbackMutationId  BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId  UNIQUEIDENTIFIER NOT NULL,
        FeedbackId          BIGINT NULL,
        FeedbackType        NVARCHAR(20) NOT NULL,
        MutationType        NVARCHAR(40) NOT NULL,
        EdgeId              BIGINT NULL,
        NodeId              BIGINT NULL,
        SubSectionId        INT NULL,
        CorrectedSubSectionId INT NULL,
        WeightDelta         DECIMAL(6,3) NULL,
        DetailsJson         NVARCHAR(MAX) NULL,
        DoctorUserId        INT NOT NULL,
        EnteredDate         DATETIME2 NOT NULL CONSTRAINT DF_AIKGFeedbackMutation_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIKGFeedbackMutation PRIMARY KEY (FeedbackMutationId)
    );
    PRINT 'Created AIKGFeedbackMutation';
END
GO

IF OBJECT_ID(N'dbo.AIKGSessionPath', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIKGSessionPath
    (
        SessionPathId       BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId  UNIQUEIDENTIFIER NOT NULL,
        SubSectionId        INT NOT NULL,
        PathJson            NVARCHAR(MAX) NOT NULL,
        PathConfidence      DECIMAL(5,4) NOT NULL,
        DiscoveryMethod     NVARCHAR(50) NOT NULL CONSTRAINT DF_AIKGSessionPath_Method DEFAULT (N'KnowledgeGraph'),
        EnteredDate         DATETIME2 NOT NULL CONSTRAINT DF_AIKGSessionPath_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIKGSessionPath PRIMARY KEY (SessionPathId)
    );
    PRINT 'Created AIKGSessionPath';
END
GO

IF OBJECT_ID(N'dbo.AIKGSessionPath', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIKGSessionPath_Session' AND object_id = OBJECT_ID(N'dbo.AIKGSessionPath'))
    CREATE INDEX IX_AIKGSessionPath_Session ON dbo.AIKGSessionPath (AudioCaseSessionId, SubSectionId);
GO

IF OBJECT_ID(N'dbo.AIKGRemedyProjection', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIKGRemedyProjection
    (
        RemedyProjectionId  BIGINT IDENTITY(1,1) NOT NULL,
        SubSectionId        INT NOT NULL,
        RemedyId            INT NOT NULL,
        GradeId             INT NULL,
        RemedyName          NVARCHAR(250) NULL,
        GradeValue          INT NULL,
        LastSyncedUtc       DATETIME2 NOT NULL CONSTRAINT DF_AIKGRemedyProjection_Synced DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIKGRemedyProjection PRIMARY KEY (RemedyProjectionId)
    );
    PRINT 'Created AIKGRemedyProjection';
END
GO

IF OBJECT_ID(N'dbo.AIKGRemedyProjection', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIKGRemedyProjection_SubSection' AND object_id = OBJECT_ID(N'dbo.AIKGRemedyProjection'))
    CREATE UNIQUE INDEX IX_AIKGRemedyProjection_SubSection ON dbo.AIKGRemedyProjection (SubSectionId, RemedyId);
GO

IF OBJECT_ID(N'dbo.AIKGFigurativeResolution', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIKGFigurativeResolution
    (
        FigurativeResolutionId BIGINT IDENTITY(1,1) NOT NULL,
        ExpressionNodeId    BIGINT NOT NULL,
        ClinicalMeaningNodeId BIGINT NOT NULL,
        ExpressionKind      NVARCHAR(30) NOT NULL,
        LiteralMeaning      NVARCHAR(1000) NULL,
        ResolutionExplanation NVARCHAR(2000) NULL,
        Confidence          DECIMAL(5,4) NOT NULL,
        Source              NVARCHAR(50) NOT NULL,
        EnteredDate         DATETIME2 NOT NULL CONSTRAINT DF_AIKGFigurativeResolution_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIKGFigurativeResolution PRIMARY KEY (FigurativeResolutionId),
        CONSTRAINT FK_AIKGFigurativeResolution_Expression FOREIGN KEY (ExpressionNodeId) REFERENCES dbo.AIKGNode (NodeId),
        CONSTRAINT FK_AIKGFigurativeResolution_Clinical FOREIGN KEY (ClinicalMeaningNodeId) REFERENCES dbo.AIKGNode (NodeId)
    );
    PRINT 'Created AIKGFigurativeResolution';
END
GO

GO

GO
PRINT '';
PRINT '>>> VERIFICATION';
GO

SELECT
    ObjectName,
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.tables t
        WHERE t.name = v.ObjectName AND t.schema_id = SCHEMA_ID(N'dbo')
    ) THEN 'OK' ELSE 'MISSING' END AS [Status]
FROM (VALUES
    (N'AudioCaseSession'),
    (N'RubricEmbeddings'),
    (N'AICaseLearning'),
    (N'AIEmbeddingVersion'),
    (N'AIMonitoringDailySnapshot'),
    (N'AIKGNode')
) v(ObjectName)
ORDER BY ObjectName;
GO

PRINT '';
PRINT '================================================================';
PRINT ' V4 ENTERPRISE ALL-IN-ONE SQL DEPLOY — FINISHED OK';
PRINT ' Next: deploy New_API + UI, then run embedding/KG API calls.';
PRINT '================================================================';
GO

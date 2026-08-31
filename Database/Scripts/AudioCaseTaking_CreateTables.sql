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

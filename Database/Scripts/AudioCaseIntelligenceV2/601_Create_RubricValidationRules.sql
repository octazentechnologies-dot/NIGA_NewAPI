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

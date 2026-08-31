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

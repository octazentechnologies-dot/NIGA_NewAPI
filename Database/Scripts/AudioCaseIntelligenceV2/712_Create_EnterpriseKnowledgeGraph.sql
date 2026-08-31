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

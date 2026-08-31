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

-- Phase 1: Clinical concepts extracted by Case Understanding Engine
-- Manual deploy only — do not auto-apply

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

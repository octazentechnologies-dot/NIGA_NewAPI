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

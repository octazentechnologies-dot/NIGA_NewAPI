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

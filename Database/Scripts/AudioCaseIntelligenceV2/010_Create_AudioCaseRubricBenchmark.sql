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

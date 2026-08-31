-- Run on HomeoCentrum_Production before resuming concept build (optional extra safety).
-- Code fix also caps SourceText at 2000 chars; this allows longer rows if needed later.

IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AIConceptEmbedding'
      AND COLUMN_NAME = 'SourceText'
      AND CHARACTER_MAXIMUM_LENGTH > 0
      AND CHARACTER_MAXIMUM_LENGTH < 4000)
BEGIN
    ALTER TABLE dbo.AIConceptEmbedding
        ALTER COLUMN SourceText NVARCHAR(4000) NOT NULL;
END
GO

-- Clear zombie concept jobs so auto-resume can start fresh
UPDATE dbo.AIEmbeddingJob
SET Status = 'Failed',
    ErrorSummary = 'Cleared for concept build resume after SourceText fix',
    CompletedAtUtc = GETUTCDATE(),
    UpdatedDate = GETDATE()
WHERE IsDeleted = 0
  AND Status = 'Running'
  AND JobType = 'ConceptOnly';
GO

SELECT COUNT(*) AS ConceptEmbeddings FROM dbo.AIConceptEmbedding;
SELECT COUNT(*) AS RunningJobs FROM dbo.AIEmbeddingJob WHERE IsDeleted = 0 AND Status = 'Running';

/*
  12 - Indexes for the repertory author-alias lookups.

  GET /api/RubricRemedy/GetGradeDetails/{subSectionId}, GetGradeDetailsWithoutGrade/{subSectionId}
  and GetRubricDetails/{subSectionId} collect the author aliases of every remedy in a subsection
  across the whole repertory (RubricRemedyDetails filtered by RemedyId). Without an index on RemedyId
  that is a full scan of RubricRemedyDetails (~1M rows) on every call.

    IX_RubricRemedyDetails_RemedyId_DeletedStatus           new index.
    IX_RemedyRubricAuthorDetails_RubricRemedyId_DeletedStatus rebuilt with AuthorId included, so the
                                                             join no longer needs a key lookup per row.

  No data changes. Safe to run more than once.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.RubricRemedyDetails')
      AND name = N'IX_RubricRemedyDetails_RemedyId_DeletedStatus'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricRemedyDetails_RemedyId_DeletedStatus
        ON dbo.RubricRemedyDetails (RemedyId, DeletedStatus);
END
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.RemedyRubricAuthorDetails')
      AND name = N'IX_RemedyRubricAuthorDetails_RubricRemedyId_DeletedStatus'
)
AND NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.RemedyRubricAuthorDetails')
      AND i.name = N'IX_RemedyRubricAuthorDetails_RubricRemedyId_DeletedStatus'
      AND ic.is_included_column = 1
      AND c.name = N'AuthorId'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RemedyRubricAuthorDetails_RubricRemedyId_DeletedStatus
        ON dbo.RemedyRubricAuthorDetails (RubricRemedyId, DeletedStatus)
        INCLUDE (AuthorId)
        WITH (DROP_EXISTING = ON);
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.RemedyRubricAuthorDetails')
      AND name = N'IX_RemedyRubricAuthorDetails_RubricRemedyId_DeletedStatus'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RemedyRubricAuthorDetails_RubricRemedyId_DeletedStatus
        ON dbo.RemedyRubricAuthorDetails (RubricRemedyId, DeletedStatus)
        INCLUDE (AuthorId);
END
GO

PRINT '12_Rubric_Remedy_Author_Indexes: done';

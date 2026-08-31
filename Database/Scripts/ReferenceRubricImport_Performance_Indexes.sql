/*
    Reference rubric bulk import performance indexes.
    Run manually on the NIGA Centrum database when convenient.
    Safe to re-run: each index is created only if it does not already exist.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SubSectionMaster_SubSectionName_DeleteStatus'
      AND object_id = OBJECT_ID('dbo.SubSectionMaster')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SubSectionMaster_SubSectionName_DeleteStatus
    ON dbo.SubSectionMaster (SubSectionName, DeleteStatus)
    INCLUDE (SubSectionId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ReferenceRubricDetails_SubSection_RefSubSection'
      AND object_id = OBJECT_ID('dbo.ReferenceRubricDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReferenceRubricDetails_SubSection_RefSubSection
    ON dbo.ReferenceRubricDetails (SubSectionId, RefSubSectionId)
    INCLUDE (DeleteStatus);
END
GO

/*
  Hot-path index for GetCommanUnCommanRubricsDetails (repertorize COMMON/UNCOMMON).
  Speeds SubSectionId lookups used by ranking; INCLUDE covers join keys.

  Run on HomeoCentrum_Dev, HomeoCentrum_Stage, and Production.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.RubricRemedyDetails', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.RubricRemedyDetails is missing.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_RubricRemedyDetails_SubSection_Active'
      AND object_id = OBJECT_ID(N'dbo.RubricRemedyDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricRemedyDetails_SubSection_Active
        ON dbo.RubricRemedyDetails (SubSectionId, DeletedStatus)
        INCLUDE (RemedyId, GradeId);
    PRINT 'Created IX_RubricRemedyDetails_SubSection_Active';
END
ELSE
    PRINT 'IX_RubricRemedyDetails_SubSection_Active OK';

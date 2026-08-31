-- Phase 7.1: Widen RubricRepertoryMap text columns
-- SubSectionMaster.SubSectionName can exceed 200 chars (hierarchical Kent rubric paths)

IF OBJECT_ID('dbo.RubricRepertoryMap', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.RubricRepertoryMap')
          AND name = 'SourceRubricKey'
          AND max_length < 4000)
    BEGIN
        ALTER TABLE dbo.RubricRepertoryMap ALTER COLUMN SourceRubricKey NVARCHAR(2000) NULL;
        PRINT 'Widened RubricRepertoryMap.SourceRubricKey to NVARCHAR(2000).';
    END

    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.RubricRepertoryMap')
          AND name = 'SourceRubricPath'
          AND max_length < 2000)
    BEGIN
        ALTER TABLE dbo.RubricRepertoryMap ALTER COLUMN SourceRubricPath NVARCHAR(1000) NULL;
        PRINT 'Widened RubricRepertoryMap.SourceRubricPath to NVARCHAR(1000).';
    END
END
GO

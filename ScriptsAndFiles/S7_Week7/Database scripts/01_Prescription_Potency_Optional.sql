/*
  S7 Week 7 — prescription potency is optional.
  Makes PrescriptionRemedyDetail.PotencyId and ErxSnapshotItem.PotencyCode nullable
  when an older environment created them as NOT NULL. Does not add a potency FK.
  Idempotent.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.PrescriptionRemedyDetail', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.PrescriptionRemedyDetail', N'PotencyId') IS NULL
    BEGIN
        ALTER TABLE dbo.PrescriptionRemedyDetail ADD PotencyId INT NULL;
        PRINT 'Added dbo.PrescriptionRemedyDetail.PotencyId INT NULL';
    END
    ELSE IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.PrescriptionRemedyDetail')
          AND name = N'PotencyId'
          AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE dbo.PrescriptionRemedyDetail ALTER COLUMN PotencyId INT NULL;
        PRINT 'Altered dbo.PrescriptionRemedyDetail.PotencyId to INT NULL';
    END
    ELSE
        PRINT 'dbo.PrescriptionRemedyDetail.PotencyId already nullable';
END
ELSE
    PRINT 'dbo.PrescriptionRemedyDetail missing — skipped PotencyId';
GO

IF OBJECT_ID(N'dbo.ErxSnapshotItem', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ErxSnapshotItem', N'PotencyCode') IS NULL
    BEGIN
        ALTER TABLE dbo.ErxSnapshotItem ADD PotencyCode NVARCHAR(20) NULL;
        PRINT 'Added dbo.ErxSnapshotItem.PotencyCode NVARCHAR(20) NULL';
    END
    ELSE IF EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.ErxSnapshotItem')
          AND name = N'PotencyCode'
          AND is_nullable = 0
    )
    BEGIN
        ALTER TABLE dbo.ErxSnapshotItem ALTER COLUMN PotencyCode NVARCHAR(20) NULL;
        PRINT 'Altered dbo.ErxSnapshotItem.PotencyCode to NVARCHAR(20) NULL';
    END
    ELSE
        PRINT 'dbo.ErxSnapshotItem.PotencyCode already nullable';
END
ELSE
    PRINT 'dbo.ErxSnapshotItem missing — skipped PotencyCode';
GO

DECLARE @sql nvarchar(max);
SELECT @sql = STRING_AGG(
    N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id))
    + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';',
    CHAR(10)
)
FROM sys.check_constraints
WHERE parent_object_id IN (OBJECT_ID(N'dbo.PrescriptionRemedyDetail'), OBJECT_ID(N'dbo.ErxSnapshotItem'))
  AND (
        definition LIKE N'%PotencyId%'
     OR definition LIKE N'%PotencyCode%'
  );

IF @sql IS NOT NULL
BEGIN
    EXEC sp_executesql @sql;
    PRINT 'Dropped potency CHECK constraints';
END
ELSE
    PRINT 'No potency CHECK constraints';
GO

PRINT '--- potency nullability ---';
SELECT
    OBJECT_SCHEMA_NAME(c.object_id) + N'.' + OBJECT_NAME(c.object_id) + N'.' + c.name AS Col,
    t.name AS TypeName,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE (c.object_id = OBJECT_ID(N'dbo.PrescriptionRemedyDetail') AND c.name = N'PotencyId')
   OR (c.object_id = OBJECT_ID(N'dbo.ErxSnapshotItem') AND c.name = N'PotencyCode');
GO

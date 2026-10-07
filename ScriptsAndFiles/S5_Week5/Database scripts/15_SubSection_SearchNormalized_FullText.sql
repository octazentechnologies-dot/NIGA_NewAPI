/*
    SubSection search support — run manually on production/staging (HomeoCentrum_Production database).

    Creates:
      - dbo.fn_NormalizeSubSectionSearch  (shared normalization)
      - SubSectionMaster.SearchNormalized column (if missing)
      - Backfill of SearchNormalized for all rows
      - INSERT/UPDATE trigger to keep SearchNormalized in sync
      - Full-text catalog + index on SearchNormalized (if missing)

    Safe to re-run: each step checks existence before creating.
    Point SSMS at the API database first (Dev: HomeoCentrum_Dev). Do not rely on a hardcoded USE.
    If the table already has a full-text index on SubSectionName (audio 725/726), step 6
    ADDs SearchNormalized to that index instead of skipping.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* -------------------------------------------------------------------------- */
/* 1. Normalization function (matches Old API C# NormalizeSubSectionSearchText) */
/* -------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.fn_NormalizeSubSectionSearch', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_NormalizeSubSectionSearch;
GO

CREATE FUNCTION dbo.fn_NormalizeSubSectionSearch (@input NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @r NVARCHAR(MAX);

    IF @input IS NULL OR LTRIM(RTRIM(@input)) = N''
        RETURN N'';

    SET @r = LOWER(@input);

    /* punctuation -> space */
    SET @r = REPLACE(@r, N'-', N' ');
    SET @r = REPLACE(@r, N',', N' ');
    SET @r = REPLACE(@r, N'.', N' ');
    SET @r = REPLACE(@r, N':', N' ');
    SET @r = REPLACE(@r, N';', N' ');

    /* digit + pm/am without space, e.g. 3pm -> 3 pm */
    SET @r = REPLACE(@r, N'0pm', N'0 pm');
    SET @r = REPLACE(@r, N'1pm', N'1 pm');
    SET @r = REPLACE(@r, N'2pm', N'2 pm');
    SET @r = REPLACE(@r, N'3pm', N'3 pm');
    SET @r = REPLACE(@r, N'4pm', N'4 pm');
    SET @r = REPLACE(@r, N'5pm', N'5 pm');
    SET @r = REPLACE(@r, N'6pm', N'6 pm');
    SET @r = REPLACE(@r, N'7pm', N'7 pm');
    SET @r = REPLACE(@r, N'8pm', N'8 pm');
    SET @r = REPLACE(@r, N'9pm', N'9 pm');
    SET @r = REPLACE(@r, N'0am', N'0 am');
    SET @r = REPLACE(@r, N'1am', N'1 am');
    SET @r = REPLACE(@r, N'2am', N'2 am');
    SET @r = REPLACE(@r, N'3am', N'3 am');
    SET @r = REPLACE(@r, N'4am', N'4 am');
    SET @r = REPLACE(@r, N'5am', N'5 am');
    SET @r = REPLACE(@r, N'6am', N'6 am');
    SET @r = REPLACE(@r, N'7am', N'7 am');
    SET @r = REPLACE(@r, N'8am', N'8 am');
    SET @r = REPLACE(@r, N'9am', N'9 am');

    /* collapse whitespace */
    WHILE CHARINDEX(N'  ', @r) > 0
        SET @r = REPLACE(@r, N'  ', N' ');

    RETURN LTRIM(RTRIM(@r));
END;
GO

/* -------------------------------------------------------------------------- */
/* 2. SearchNormalized column                                                 */
/* -------------------------------------------------------------------------- */
IF COL_LENGTH(N'dbo.SubSectionMaster', N'SearchNormalized') IS NULL
BEGIN
    ALTER TABLE dbo.SubSectionMaster
        ADD SearchNormalized NVARCHAR(MAX) NULL;

    PRINT 'Added column dbo.SubSectionMaster.SearchNormalized';
END
ELSE
    PRINT 'Column dbo.SubSectionMaster.SearchNormalized already exists';
GO

/* -------------------------------------------------------------------------- */
/* 3. Backfill (all rows; safe to re-run)                                     */
/* -------------------------------------------------------------------------- */
UPDATE dbo.SubSectionMaster
SET SearchNormalized = dbo.fn_NormalizeSubSectionSearch(SubSectionName)
WHERE DeleteStatus = 0
  AND (
        SearchNormalized IS NULL
        OR SearchNormalized <> dbo.fn_NormalizeSubSectionSearch(SubSectionName)
      );

PRINT CONCAT('Backfill updated rows: ', @@ROWCOUNT);
GO

/* -------------------------------------------------------------------------- */
/* 4. Trigger — keep SearchNormalized updated on INSERT/UPDATE                */
/* -------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TR_SubSectionMaster_SearchNormalized', N'TR') IS NOT NULL
    DROP TRIGGER dbo.TR_SubSectionMaster_SearchNormalized;
GO

CREATE TRIGGER dbo.TR_SubSectionMaster_SearchNormalized
ON dbo.SubSectionMaster
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE s
    SET SearchNormalized = dbo.fn_NormalizeSubSectionSearch(i.SubSectionName)
    FROM dbo.SubSectionMaster s
    INNER JOIN inserted i ON s.SubSectionID = i.SubSectionID
    WHERE i.SubSectionName IS NOT NULL;
END;
GO

PRINT 'Trigger dbo.TR_SubSectionMaster_SearchNormalized created';
GO

/* -------------------------------------------------------------------------- */
/* 5. Full-text catalog                                                       */
/* -------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'FT_SubSectionCatalog')
BEGIN
    CREATE FULLTEXT CATALOG FT_SubSectionCatalog AS DEFAULT;
    PRINT 'Created full-text catalog FT_SubSectionCatalog';
END
ELSE
    PRINT 'Full-text catalog FT_SubSectionCatalog already exists';
GO

/* -------------------------------------------------------------------------- */
/* 6. Full-text index on SearchNormalized                                     */
/* -------------------------------------------------------------------------- */
DECLARE @PkIndexName SYSNAME;
DECLARE @sql NVARCHAR(MAX);

SELECT TOP (1) @PkIndexName = i.name
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'dbo.SubSectionMaster')
  AND i.is_primary_key = 1;

IF @PkIndexName IS NULL
BEGIN
    RAISERROR('Primary key index not found on dbo.SubSectionMaster', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.fulltext_indexes fti
    WHERE fti.object_id = OBJECT_ID(N'dbo.SubSectionMaster')
)
BEGIN
    SET @sql = N'
CREATE FULLTEXT INDEX ON dbo.SubSectionMaster(SearchNormalized)
    KEY INDEX ' + QUOTENAME(@PkIndexName) + N'
    ON FT_SubSectionCatalog
    WITH CHANGE_TRACKING AUTO;';

    EXEC sp_executesql @sql;
    PRINT CONCAT('Created full-text index on SearchNormalized using key ', @PkIndexName);
END
ELSE IF NOT EXISTS (
    SELECT 1
    FROM sys.fulltext_index_columns fic
    INNER JOIN sys.columns c
        ON c.object_id = fic.object_id AND c.column_id = fic.column_id
    WHERE fic.object_id = OBJECT_ID(N'dbo.SubSectionMaster')
      AND c.name = N'SearchNormalized'
)
BEGIN
    SET @sql = N'ALTER FULLTEXT INDEX ON dbo.SubSectionMaster ADD (SearchNormalized LANGUAGE 1033);';
    EXEC sp_executesql @sql;
    PRINT 'Added SearchNormalized to the existing dbo.SubSectionMaster full-text index';
END
ELSE
    PRINT 'Full-text index on dbo.SubSectionMaster already includes SearchNormalized';
GO

/* -------------------------------------------------------------------------- */
/* 7. Verification                                                            */
/* -------------------------------------------------------------------------- */
SELECT
    TotalRows = COUNT(*),
    WithSearchNormalized = SUM(CASE WHEN SearchNormalized IS NOT NULL AND SearchNormalized <> N'' THEN 1 ELSE 0 END),
    MissingSearchNormalized = SUM(CASE WHEN SearchNormalized IS NULL OR SearchNormalized = N'' THEN 1 ELSE 0 END)
FROM dbo.SubSectionMaster
WHERE DeleteStatus = 0;

SELECT TOP (5)
    SubSectionID,
    SubSectionName,
    SearchNormalized
FROM dbo.SubSectionMaster
WHERE DeleteStatus = 0
  AND SearchNormalized LIKE N'%afternoon%'
ORDER BY SubSectionID;
GO

PRINT 'SubSection_SearchNormalized_Setup.sql completed successfully.';
GO

-- Diagnose + fix: Msg 7601 — SubSectionName not full-text indexed
-- Run against the SAME database the API uses (check connection dropdown first).

-- 0) Confirm database
SELECT DB_NAME() AS CurrentDatabase;

-- 1) Does a full-text index exist on SubSectionMaster, and on which columns?
SELECT
    OBJECT_NAME(fic.object_id) AS TableName,
    c.name AS ColumnName,
    fi.is_enabled,
    fi.change_tracking_state_desc,
    fi.has_crawl_completed
FROM sys.fulltext_index_columns fic
INNER JOIN sys.columns c
    ON c.object_id = fic.object_id AND c.column_id = fic.column_id
INNER JOIN sys.fulltext_indexes fi
    ON fi.object_id = fic.object_id
WHERE fic.object_id = OBJECT_ID(N'dbo.SubSectionMaster');

-- If this returns ZERO rows, or no row for SubSectionName → that is why CONTAINS fails.
-- Even if sys.fulltext_indexes shows a row for the table, the COLUMN may be missing.

-- 2) Catalog present?
SELECT name FROM sys.fulltext_catalogs WHERE name = N'ftCatalog_NigaHomeopathy';
GO

-- ========== FIX ==========
-- Drop existing FTS index on SubSectionMaster (if any), then recreate on SubSectionName.

IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ftCatalog_NigaHomeopathy')
    CREATE FULLTEXT CATALOG ftCatalog_NigaHomeopathy AS DEFAULT;
GO

IF EXISTS (
    SELECT 1 FROM sys.fulltext_indexes
    WHERE object_id = OBJECT_ID(N'dbo.SubSectionMaster'))
BEGIN
    DROP FULLTEXT INDEX ON dbo.SubSectionMaster;
END
GO

DECLARE @PkName SYSNAME =
(
    SELECT TOP 1 i.name
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'dbo.SubSectionMaster')
      AND i.is_primary_key = 1
);

IF @PkName IS NULL
BEGIN
    RAISERROR(N'No PK on dbo.SubSectionMaster — cannot create FULLTEXT INDEX.', 16, 1);
    RETURN;
END

DECLARE @sql NVARCHAR(MAX) = N'
CREATE FULLTEXT INDEX ON dbo.SubSectionMaster (SubSectionName LANGUAGE 1033)
KEY INDEX ' + QUOTENAME(@PkName) + N'
ON ftCatalog_NigaHomeopathy
WITH CHANGE_TRACKING AUTO;';

EXEC sp_executesql @sql;
GO

-- Wait for crawl (small DBs: seconds; 185k rows: often under a few minutes)
DECLARE @i INT = 0;
WHILE @i < 60
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.fulltext_indexes
        WHERE object_id = OBJECT_ID(N'dbo.SubSectionMaster')
          AND has_crawl_completed = 1
          AND is_enabled = 1)
        BREAK;

    WAITFOR DELAY '00:00:02';
    SET @i += 1;
END

-- 3) Verify column is indexed
SELECT
    c.name AS ColumnName,
    fi.is_enabled,
    fi.has_crawl_completed
FROM sys.fulltext_index_columns fic
INNER JOIN sys.columns c
    ON c.object_id = fic.object_id AND c.column_id = fic.column_id
INNER JOIN sys.fulltext_indexes fi
    ON fi.object_id = fic.object_id
WHERE fic.object_id = OBJECT_ID(N'dbo.SubSectionMaster');

-- 4) Smoke test (run only after ColumnName = SubSectionName and has_crawl_completed = 1)
SELECT TOP 20 SubSectionId, SubSectionName
FROM dbo.SubSectionMaster
WHERE DeleteStatus = 0
  AND CONTAINS(SubSectionName, N'"talking*" AND "sleep*"');
GO

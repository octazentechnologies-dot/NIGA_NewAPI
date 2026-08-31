-- Full-text index for SubSectionMaster.SubSectionName
-- LIKE '%term%' cannot use a normal B-tree index and full-scans a large repertory table,
-- which caused 15s per-concept keyword timeouts (Part 3).

IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = N'ftCatalog_NigaHomeopathy')
BEGIN
    CREATE FULLTEXT CATALOG ftCatalog_NigaHomeopathy AS DEFAULT;
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
    RAISERROR(N'No primary key found on dbo.SubSectionMaster — cannot create FULLTEXT INDEX.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.fulltext_indexes
    WHERE object_id = OBJECT_ID(N'dbo.SubSectionMaster'))
BEGIN
    DECLARE @sql NVARCHAR(MAX) = N'
    CREATE FULLTEXT INDEX ON dbo.SubSectionMaster (SubSectionName LANGUAGE 1033)
    KEY INDEX ' + QUOTENAME(@PkName) + N'
    ON ftCatalog_NigaHomeopathy
    WITH CHANGE_TRACKING AUTO;';
    EXEC sp_executesql @sql;
END
GO

SELECT COUNT(*) AS SubSectionRowCount
FROM dbo.SubSectionMaster
WHERE DeleteStatus = 0 AND SubSectionName IS NOT NULL;

SELECT
    OBJECT_NAME(object_id) AS TableName,
    is_enabled,
    change_tracking_state_desc,
    has_crawl_completed
FROM sys.fulltext_indexes
WHERE object_id = OBJECT_ID(N'dbo.SubSectionMaster');
GO

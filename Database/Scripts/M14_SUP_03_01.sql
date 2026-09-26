/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M14_SUP_03_01.sql
Purpose      : SUP-03.01 — extend dbo.SupportTicket for admin issue queue:
               Priority, AssigneeUserId, SlaDueAt (plus Status for filtering).
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.SupportTicket (SUP-01.01).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.SupportTicket', N'U') IS NULL
BEGIN
    RAISERROR('SUP-03.01 requires dbo.SupportTicket (run M14_SUP_01_01.sql first).', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.SupportTicket', N'Priority') IS NULL
    ALTER TABLE dbo.SupportTicket ADD Priority NVARCHAR(20) NOT NULL
        CONSTRAINT DF_SupportTicket_Priority_Add DEFAULT (N'NORMAL');

IF COL_LENGTH(N'dbo.SupportTicket', N'AssigneeUserId') IS NULL
    ALTER TABLE dbo.SupportTicket ADD AssigneeUserId BIGINT NULL;

IF COL_LENGTH(N'dbo.SupportTicket', N'SlaDueAt') IS NULL
    ALTER TABLE dbo.SupportTicket ADD SlaDueAt DATETIME NULL;

IF COL_LENGTH(N'dbo.SupportTicket', N'Status') IS NULL
    ALTER TABLE dbo.SupportTicket ADD Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_SupportTicket_Status_Add DEFAULT (N'OPEN');
GO

-- Defaults when table pre-existed without them
IF COL_LENGTH(N'dbo.SupportTicket', N'Priority') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SupportTicket')
          AND c.name = N'Priority'
   )
    ALTER TABLE dbo.SupportTicket
        ADD CONSTRAINT DF_SupportTicket_Priority DEFAULT (N'NORMAL') FOR Priority;
GO

IF COL_LENGTH(N'dbo.SupportTicket', N'Status') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.SupportTicket')
          AND c.name = N'Status'
   )
    ALTER TABLE dbo.SupportTicket
        ADD CONSTRAINT DF_SupportTicket_Status DEFAULT (N'OPEN') FOR Status;
GO

-- Backfill SLA for open tickets missing due date (24h from CreatedAt)
UPDATE dbo.SupportTicket
SET SlaDueAt = DATEADD(HOUR, 24, CreatedAt)
WHERE SlaDueAt IS NULL
  AND Status <> N'Closed'
  AND Status <> N'CLOSED';
GO

-- Admin queue filter index: status + priority + assignee + SLA
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SupportTicket')
      AND name = N'IX_SupportTicket_AdminQueue'
)
    CREATE INDEX IX_SupportTicket_AdminQueue
        ON dbo.SupportTicket (Status, Priority, AssigneeUserId, SlaDueAt);
GO

IF COL_LENGTH(N'dbo.SupportTicket', N'Priority') IS NULL
   OR COL_LENGTH(N'dbo.SupportTicket', N'AssigneeUserId') IS NULL
   OR COL_LENGTH(N'dbo.SupportTicket', N'SlaDueAt') IS NULL
BEGIN
    RAISERROR('SUP-03.01 required columns Priority, AssigneeUserId, SlaDueAt are missing.', 16, 1);
    RETURN;
END
GO

PRINT 'M14_SUP_03_01.sql: SupportTicket extended (Priority, Assignee, SLA).';

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable,
    dc.definition AS DefaultDefinition
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc
    ON dc.parent_object_id = c.object_id
   AND dc.parent_column_id = c.column_id
WHERE c.object_id = OBJECT_ID(N'dbo.SupportTicket')
  AND c.name IN (N'Priority', N'AssigneeUserId', N'SlaDueAt', N'Status')
ORDER BY c.column_id;

SELECT TOP 5
    SupportTicketId,
    Status,
    Priority,
    AssigneeUserId,
    SlaDueAt
FROM dbo.SupportTicket
ORDER BY SupportTicketId DESC;
GO

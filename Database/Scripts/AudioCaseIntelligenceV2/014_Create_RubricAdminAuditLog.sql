-- Admin audit trail for metaphor/alias CRUD

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricAdminAuditLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricAdminAuditLog
    (
        AuditLogId     BIGINT IDENTITY(1,1) NOT NULL,
        EntityType     NVARCHAR(50) NOT NULL,
        EntityId       BIGINT NOT NULL,
        ActionType     NVARCHAR(30) NOT NULL,
        BeforeJson     NVARCHAR(MAX) NULL,
        AfterJson      NVARCHAR(MAX) NULL,
        AdminUserId    INT NOT NULL,
        IpAddress      NVARCHAR(45) NULL,
        EnteredDate    DATETIME NOT NULL CONSTRAINT DF_RubricAdminAuditLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_RubricAdminAuditLog PRIMARY KEY (AuditLogId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_RubricAdminAuditLog_Entity
    ON dbo.RubricAdminAuditLog (EntityType, EntityId, EnteredDate DESC);
GO

/*
  09 - Tamper-evident security audit trail (logins, failed logins, token issuance, payout OTPs,
       patient-data exports, blocked uploads).

  Every row carries:
    Mac      = HMAC-SHA256 of the row fields, computed by the API with a key the database never sees.
    PrevHash = RowHash of the previous row.
    RowHash  = SHA-256( PrevHash | Canonical | Mac ), computed here under a lock so the chain has no gaps.

  Editing or deleting any row breaks the chain from that row on; GET /api/Admin/SecurityAudit/Verify reports
  the first broken row. UPDATE and DELETE are refused by trigger. Safe to run more than once.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.SecurityAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SecurityAuditLog
    (
        SecurityAuditLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SecurityAuditLog PRIMARY KEY,
        OccurredAtUtc      DATETIME2(3)   NOT NULL,
        EventType          VARCHAR(60)    NOT NULL,
        Outcome            VARCHAR(20)    NOT NULL,
        ActorUserId        BIGINT         NULL,
        ActorRole          NVARCHAR(50)   NULL,
        Subject            NVARCHAR(150)  NULL,
        SourceApi          VARCHAR(20)    NOT NULL,
        ClientIp           VARCHAR(64)    NULL,
        CorrelationId      NVARCHAR(80)   NULL,
        Detail             NVARCHAR(500)  NULL,
        KeyId              VARCHAR(20)    NOT NULL,
        Mac                CHAR(64)       NOT NULL,
        PrevHash           CHAR(64)       NOT NULL,
        RowHash            CHAR(64)       NOT NULL
    );
    CREATE INDEX IX_SecurityAuditLog_Time ON dbo.SecurityAuditLog (OccurredAtUtc DESC);
    CREATE INDEX IX_SecurityAuditLog_Event ON dbo.SecurityAuditLog (EventType, OccurredAtUtc DESC);
    CREATE INDEX IX_SecurityAuditLog_Actor ON dbo.SecurityAuditLog (ActorUserId, OccurredAtUtc DESC);
END
GO

CREATE OR ALTER TRIGGER dbo.TR_SecurityAuditLog_NoChange
ON dbo.SecurityAuditLog
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, 'SecurityAuditLog is append-only. Rows cannot be updated or deleted.', 1;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_SecurityAudit_Append
    @OccurredAtUtc DATETIME2(3),
    @EventType     VARCHAR(60),
    @Outcome       VARCHAR(20),
    @ActorUserId   BIGINT,
    @ActorRole     NVARCHAR(50),
    @Subject       NVARCHAR(150),
    @SourceApi     VARCHAR(20),
    @ClientIp      VARCHAR(64),
    @CorrelationId NVARCHAR(80),
    @Detail        NVARCHAR(500),
    @KeyId         VARCHAR(20),
    @Canonical     NVARCHAR(2000),
    @Mac           CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Prev CHAR(64) =
        (SELECT TOP (1) RowHash FROM dbo.SecurityAuditLog WITH (UPDLOCK, HOLDLOCK) ORDER BY SecurityAuditLogId DESC);
    IF @Prev IS NULL SET @Prev = REPLICATE('0', 64);

    DECLARE @RowHash CHAR(64) =
        CONVERT(CHAR(64), HASHBYTES('SHA2_256', CONCAT(CAST(@Prev AS NVARCHAR(64)), N'|', @Canonical, N'|', CAST(@Mac AS NVARCHAR(64)))), 2);

    INSERT dbo.SecurityAuditLog
        (OccurredAtUtc, EventType, Outcome, ActorUserId, ActorRole, Subject, SourceApi, ClientIp, CorrelationId, Detail, KeyId, Mac, PrevHash, RowHash)
    VALUES
        (@OccurredAtUtc, @EventType, @Outcome, @ActorUserId, @ActorRole, @Subject, @SourceApi, @ClientIp, @CorrelationId, @Detail, @KeyId, @Mac, @Prev, @RowHash);

    COMMIT TRANSACTION;
END
GO

PRINT '09_Security_Audit_Log applied.';

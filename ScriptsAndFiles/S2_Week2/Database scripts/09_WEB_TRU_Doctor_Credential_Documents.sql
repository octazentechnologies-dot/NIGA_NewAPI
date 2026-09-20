/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 09_WEB_TRU_Doctor_Credential_Documents.sql
Purpose      : WEB-09.01 — reuse TRU-01 document tables:
               DoctorVerification (Pending/Approved/Rejected/NeedsInfo)
               DoctorCredentialDocument (qualification / registration files).
Use          : Run after 01–08 on HomeoCentrum_Dev.
Idempotent   : Yes. Creates tables/indexes if missing. Backfills one verification
               row per existing doctor.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Doctor', N'U') IS NULL
BEGIN
    RAISERROR('dbo.Doctor is missing. Stop.', 16, 1);
    RETURN;
END

IF OBJECT_ID(N'dbo.DoctorVerification', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorVerification
    (
        DoctorVerificationId INT IDENTITY(1, 1) NOT NULL,
        DoctorId INT NOT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_DoctorVerification_Status DEFAULT (N'Pending'),
        ReviewerNote NVARCHAR(1000) NULL,
        ReviewedBy INT NULL,
        ReviewedAt DATETIME NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_DoctorVerification_EnteredDate DEFAULT (GETUTCDATE()),
        ChangedDate DATETIME NULL,
        DeleteStatus BIT NOT NULL CONSTRAINT DF_DoctorVerification_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_DoctorVerification PRIMARY KEY (DoctorVerificationId),
        CONSTRAINT FK_DoctorVerification_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.Doctor (DoctorId),
        CONSTRAINT CK_DoctorVerification_Status CHECK (Status IN (N'Pending', N'Approved', N'Rejected', N'NeedsInfo'))
    );
    PRINT 'CREATED dbo.DoctorVerification';
END
ELSE
    PRINT 'SKIP dbo.DoctorVerification already exists';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_DoctorVerification_Doctor_Active'
      AND object_id = OBJECT_ID(N'dbo.DoctorVerification')
)
    CREATE UNIQUE INDEX UX_DoctorVerification_Doctor_Active
        ON dbo.DoctorVerification (DoctorId)
        WHERE DeleteStatus = 0;

IF OBJECT_ID(N'dbo.DoctorCredentialDocument', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorCredentialDocument
    (
        DoctorCredentialDocumentId INT IDENTITY(1, 1) NOT NULL,
        DoctorId INT NOT NULL,
        DoctorVerificationId INT NULL,
        DocumentType NVARCHAR(50) NOT NULL,
        FileName NVARCHAR(260) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        ContentType NVARCHAR(100) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_DoctorCredentialDocument_EnteredDate DEFAULT (GETUTCDATE()),
        DeleteStatus BIT NOT NULL CONSTRAINT DF_DoctorCredentialDocument_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_DoctorCredentialDocument PRIMARY KEY (DoctorCredentialDocumentId),
        CONSTRAINT FK_DoctorCredentialDocument_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.Doctor (DoctorId),
        CONSTRAINT FK_DoctorCredentialDocument_Verification FOREIGN KEY (DoctorVerificationId)
            REFERENCES dbo.DoctorVerification (DoctorVerificationId)
    );
    PRINT 'CREATED dbo.DoctorCredentialDocument';
END
ELSE
    PRINT 'SKIP dbo.DoctorCredentialDocument already exists';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_DoctorCredentialDocument_Doctor'
      AND object_id = OBJECT_ID(N'dbo.DoctorCredentialDocument')
)
    CREATE INDEX IX_DoctorCredentialDocument_Doctor
        ON dbo.DoctorCredentialDocument (DoctorId, DeleteStatus);

INSERT INTO dbo.DoctorVerification (DoctorId, Status, EnteredDate, DeleteStatus)
SELECT
    d.DoctorId,
    CASE
        WHEN d.VerificationStatus = N'Verified' THEN N'Approved'
        WHEN d.VerificationStatus IN (N'Rejected', N'NeedsInfo') THEN d.VerificationStatus
        ELSE N'Pending'
    END,
    GETUTCDATE(),
    0
FROM dbo.Doctor d
WHERE ISNULL(d.DeleteStatus, 0) = 0
  AND NOT EXISTS (
        SELECT 1
        FROM dbo.DoctorVerification v
        WHERE v.DoctorId = d.DoctorId
          AND ISNULL(v.DeleteStatus, 0) = 0
    );

DECLARE @V INT = (SELECT COUNT(*) FROM dbo.DoctorVerification WHERE ISNULL(DeleteStatus, 0) = 0);
DECLARE @D INT = (SELECT COUNT(*) FROM dbo.DoctorCredentialDocument WHERE ISNULL(DeleteStatus, 0) = 0);
PRINT CONCAT('DoctorVerification active rows = ', @V);
PRINT CONCAT('DoctorCredentialDocument active rows = ', @D);
PRINT '09_WEB_TRU_Doctor_Credential_Documents OK';

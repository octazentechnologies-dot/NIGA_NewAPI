/*
================================================================================
Author       : Tufan Powar
Created      : 05-10-2026
Script       : 25_WEB_Doctor_Credential_FilePath.sql
Purpose      : Optional doctor media. DoctorCredentialDocument.FilePath stores
               the saved file path. Creates the table when it is missing and
               adds FilePath when an older table does not have it.
Use          : Run on HomeoCentrum_Dev after 09_WEB_TRU_Doctor_Credential_Documents.sql.
Idempotent   : Yes.
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
        CONSTRAINT PK_DoctorCredentialDocument PRIMARY KEY (DoctorCredentialDocumentId)
    );
    PRINT 'CREATED dbo.DoctorCredentialDocument with FilePath';
END
ELSE
    PRINT 'SKIP create dbo.DoctorCredentialDocument';

IF COL_LENGTH(N'dbo.DoctorCredentialDocument', N'FilePath') IS NULL
BEGIN
    ALTER TABLE dbo.DoctorCredentialDocument ADD FilePath NVARCHAR(500) NULL;
    PRINT 'ADDED DoctorCredentialDocument.FilePath';
END
ELSE
    PRINT 'SKIP FilePath already present';

IF COL_LENGTH(N'dbo.DoctorCredentialDocument', N'FileName') IS NULL
    ALTER TABLE dbo.DoctorCredentialDocument ADD FileName NVARCHAR(260) NULL;

IF COL_LENGTH(N'dbo.DoctorCredentialDocument', N'ContentType') IS NULL
    ALTER TABLE dbo.DoctorCredentialDocument ADD ContentType NVARCHAR(100) NULL;

IF COL_LENGTH(N'dbo.DoctorCredentialDocument', N'DocumentType') IS NULL
    ALTER TABLE dbo.DoctorCredentialDocument ADD DocumentType NVARCHAR(50) NULL;

PRINT '25_WEB_Doctor_Credential_FilePath OK';

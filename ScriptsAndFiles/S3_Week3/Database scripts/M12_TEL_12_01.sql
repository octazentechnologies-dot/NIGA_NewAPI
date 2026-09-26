/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M12_TEL_12_01.sql
Purpose      : TEL-12.01 — Instant consultation persistence.
               1) dbo.InstantConsultRequest — patient request / queue
                  (PatientId, ContactName, ContactMobile, Status, QueuePosition, CreatedAt).
               2) dbo.DoctorOffer — offer to an online doctor
                  (InstantConsultRequestId, DoctorId, Status, At).
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: Soft links by PatientId / DoctorId (FK optional).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* --- InstantConsultRequest --- */
IF OBJECT_ID(N'dbo.InstantConsultRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InstantConsultRequest
    (
        InstantConsultRequestId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InstantConsultRequest PRIMARY KEY,
        PatientId INT NULL,
        ContactName NVARCHAR(200) NOT NULL,
        ContactMobile NVARCHAR(30) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_InstantConsultRequest_Status DEFAULT (N'OPEN'),
        QueuePosition INT NOT NULL CONSTRAINT DF_InstantConsultRequest_Queue DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_InstantConsultRequest_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientId') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD PatientId INT NULL;
IF COL_LENGTH(N'dbo.InstantConsultRequest', N'ContactName') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD ContactName NVARCHAR(200) NOT NULL
        CONSTRAINT DF_InstantConsultRequest_ContactName_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.InstantConsultRequest', N'ContactMobile') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD ContactMobile NVARCHAR(30) NOT NULL
        CONSTRAINT DF_InstantConsultRequest_ContactMobile_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.InstantConsultRequest', N'Status') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_InstantConsultRequest_Status_Add DEFAULT (N'OPEN');
IF COL_LENGTH(N'dbo.InstantConsultRequest', N'QueuePosition') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD QueuePosition INT NOT NULL
        CONSTRAINT DF_InstantConsultRequest_Queue_Add DEFAULT (1);
IF COL_LENGTH(N'dbo.InstantConsultRequest', N'CreatedAt') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD CreatedAt DATETIME NOT NULL
        CONSTRAINT DF_InstantConsultRequest_CreatedAt_Add DEFAULT (GETDATE());
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.InstantConsultRequest')
      AND name = N'IX_InstantConsultRequest_Status_Queue'
)
    CREATE INDEX IX_InstantConsultRequest_Status_Queue
        ON dbo.InstantConsultRequest (Status, QueuePosition, InstantConsultRequestId);
GO

/* --- DoctorOffer --- */
IF OBJECT_ID(N'dbo.DoctorOffer', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorOffer
    (
        DoctorOfferId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DoctorOffer PRIMARY KEY,
        InstantConsultRequestId INT NOT NULL,
        DoctorId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_DoctorOffer_At DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.DoctorOffer', N'InstantConsultRequestId') IS NULL
    ALTER TABLE dbo.DoctorOffer ADD InstantConsultRequestId INT NOT NULL
        CONSTRAINT DF_DoctorOffer_RequestId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.DoctorOffer', N'DoctorId') IS NULL
    ALTER TABLE dbo.DoctorOffer ADD DoctorId INT NOT NULL
        CONSTRAINT DF_DoctorOffer_DoctorId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.DoctorOffer', N'Status') IS NULL
    ALTER TABLE dbo.DoctorOffer ADD Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_DoctorOffer_Status_Add DEFAULT (N'OFFERED');
IF COL_LENGTH(N'dbo.DoctorOffer', N'At') IS NULL
    ALTER TABLE dbo.DoctorOffer ADD At DATETIME NOT NULL
        CONSTRAINT DF_DoctorOffer_At_Add DEFAULT (GETDATE());
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DoctorOffer')
      AND name = N'IX_DoctorOffer_Doctor_Status'
)
    CREATE INDEX IX_DoctorOffer_Doctor_Status
        ON dbo.DoctorOffer (DoctorId, Status, InstantConsultRequestId);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DoctorOffer')
      AND name = N'IX_DoctorOffer_RequestId'
)
    CREATE INDEX IX_DoctorOffer_RequestId
        ON dbo.DoctorOffer (InstantConsultRequestId);
GO

PRINT 'M12_TEL_12_01.sql: InstantConsultRequest + DoctorOffer ready.';

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.InstantConsultRequest')
ORDER BY c.column_id;

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.DoctorOffer')
ORDER BY c.column_id;

SELECT TOP 5
    InstantConsultRequestId,
    PatientId,
    ContactName,
    ContactMobile,
    Status,
    QueuePosition,
    CreatedAt
FROM dbo.InstantConsultRequest
ORDER BY InstantConsultRequestId DESC;

SELECT TOP 5
    DoctorOfferId,
    InstantConsultRequestId,
    DoctorId,
    Status,
    At
FROM dbo.DoctorOffer
ORDER BY DoctorOfferId DESC;
GO

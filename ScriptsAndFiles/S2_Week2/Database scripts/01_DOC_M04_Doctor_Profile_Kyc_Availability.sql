/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 01_DOC_M04_Doctor_Profile_Kyc_Availability.sql
Purpose      : DOC-10.01 doctor profile/fee/photo fields, DoctorPayeeKyc (bank/PAN),
               DMO-05 availability (IsOnline). WEB-03 directory flags on Doctor.
Use          : Run first on HomeoCentrum_Dev. Existing practising doctors are marked
               DirectoryVisible so public search is not empty on Dev.
Prerequisites: dbo.Doctor exists.
Idempotent   : Yes. Adds missing columns/tables/indexes; does not drop data.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.Doctor', N'U') IS NULL
BEGIN
    RAISERROR('dbo.Doctor is missing. Stop.', 16, 1);
    RETURN;
END
GO

-- DOC-10.01 / WEB-03 — profile + public directory columns
IF COL_LENGTH(N'dbo.Doctor', N'ClinicName') IS NULL
    ALTER TABLE dbo.Doctor ADD ClinicName NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.Doctor', N'ConsultFeeInClinic') IS NULL
    ALTER TABLE dbo.Doctor ADD ConsultFeeInClinic DECIMAL(10,2) NULL;
IF COL_LENGTH(N'dbo.Doctor', N'ConsultFeeTele') IS NULL
    ALTER TABLE dbo.Doctor ADD ConsultFeeTele DECIMAL(10,2) NULL;
IF COL_LENGTH(N'dbo.Doctor', N'PhotoPath') IS NULL
    ALTER TABLE dbo.Doctor ADD PhotoPath NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.Doctor', N'WorkingHoursNote') IS NULL
    ALTER TABLE dbo.Doctor ADD WorkingHoursNote NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.Doctor', N'IsOnline') IS NULL
    ALTER TABLE dbo.Doctor ADD IsOnline BIT NOT NULL CONSTRAINT DF_Doctor_IsOnline_S2 DEFAULT (0);
IF COL_LENGTH(N'dbo.Doctor', N'DirectoryVisible') IS NULL
    ALTER TABLE dbo.Doctor ADD DirectoryVisible BIT NOT NULL CONSTRAINT DF_Doctor_DirectoryVisible_S2 DEFAULT (0);
IF COL_LENGTH(N'dbo.Doctor', N'VerificationStatus') IS NULL
    ALTER TABLE dbo.Doctor ADD VerificationStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Doctor_VerificationStatus_S2 DEFAULT (N'Pending');
IF COL_LENGTH(N'dbo.Doctor', N'PracticeActivated') IS NULL
    ALTER TABLE dbo.Doctor ADD PracticeActivated BIT NOT NULL CONSTRAINT DF_Doctor_PracticeActivated_S2 DEFAULT (0);
IF COL_LENGTH(N'dbo.Doctor', N'CountryId') IS NULL
    ALTER TABLE dbo.Doctor ADD CountryId INT NULL;
IF COL_LENGTH(N'dbo.Doctor', N'StateId') IS NULL
    ALTER TABLE dbo.Doctor ADD StateId INT NULL;
GO

-- Existing clinic doctors already practise — keep them in the public directory.
IF COL_LENGTH(N'dbo.Doctor', N'DirectoryVisible') IS NOT NULL
BEGIN
    UPDATE dbo.Doctor
    SET DirectoryVisible = 1,
        VerificationStatus = N'Verified',
        PracticeActivated = 1,
        ClinicName = COALESCE(NULLIF(LTRIM(RTRIM(ClinicName)), N''), FirstName + N' ' + LastName)
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND (DirectoryVisible = 0 OR VerificationStatus = N'Pending');
END
GO

IF OBJECT_ID(N'dbo.Doctor', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Doctor_Directory_S2' AND object_id = OBJECT_ID(N'dbo.Doctor'))
    CREATE INDEX IX_Doctor_Directory_S2
        ON dbo.Doctor (DirectoryVisible, VerificationStatus, DeleteStatus, City, IsOnline);
GO

-- DOC-10.01 — bank/PAN for later Account payouts
IF OBJECT_ID(N'dbo.DoctorPayeeKyc', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorPayeeKyc
    (
        DoctorPayeeKycId BIGINT IDENTITY(1,1) NOT NULL,
        DoctorId INT NOT NULL,
        AccountHolder NVARCHAR(200) NULL,
        BankName NVARCHAR(200) NULL,
        AccountNumber NVARCHAR(50) NULL,
        Ifsc NVARCHAR(20) NULL,
        Pan NVARCHAR(20) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_DoctorPayeeKyc_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME NULL,
        DeleteStatus BIT NOT NULL CONSTRAINT DF_DoctorPayeeKyc_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_DoctorPayeeKyc PRIMARY KEY CLUSTERED (DoctorPayeeKycId)
    );
END
GO

IF OBJECT_ID(N'dbo.DoctorPayeeKyc', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DoctorPayeeKyc_DoctorId' AND object_id = OBJECT_ID(N'dbo.DoctorPayeeKyc'))
    CREATE UNIQUE INDEX UX_DoctorPayeeKyc_DoctorId ON dbo.DoctorPayeeKyc (DoctorId) WHERE DeleteStatus = 0;
GO

PRINT '01_DOC_M04_Doctor_Profile_Kyc_Availability.sql complete';
GO

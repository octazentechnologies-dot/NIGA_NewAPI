/*
    Doctor profile fields shown on /profile that had no column.
    Safe to run again: each column is added only when it is missing.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH(N'dbo.Doctor', N'FollowUpFeeInClinic') IS NULL
    ALTER TABLE dbo.Doctor ADD FollowUpFeeInClinic decimal(10,2) NULL;
IF COL_LENGTH(N'dbo.Doctor', N'FollowUpFeeTele') IS NULL
    ALTER TABLE dbo.Doctor ADD FollowUpFeeTele decimal(10,2) NULL;
IF COL_LENGTH(N'dbo.Doctor', N'FreeFollowUpDaysInClinic') IS NULL
    ALTER TABLE dbo.Doctor ADD FreeFollowUpDaysInClinic int NULL;
IF COL_LENGTH(N'dbo.Doctor', N'FreeFollowUpDaysTele') IS NULL
    ALTER TABLE dbo.Doctor ADD FreeFollowUpDaysTele int NULL;
IF COL_LENGTH(N'dbo.Doctor', N'FeeCurrency') IS NULL
    ALTER TABLE dbo.Doctor ADD FeeCurrency nvarchar(3) NULL;
IF COL_LENGTH(N'dbo.Doctor', N'GoogleMapsLink') IS NULL
    ALTER TABLE dbo.Doctor ADD GoogleMapsLink nvarchar(500) NULL;
GO

IF COL_LENGTH(N'dbo.DoctorPayeeKyc', N'BranchName') IS NULL
    ALTER TABLE dbo.DoctorPayeeKyc ADD BranchName nvarchar(150) NULL;
IF COL_LENGTH(N'dbo.DoctorPayeeKyc', N'AccountType') IS NULL
    ALTER TABLE dbo.DoctorPayeeKyc ADD AccountType nvarchar(20) NULL;
GO

IF COL_LENGTH(N'dbo.DoctorCredentialDocument', N'Degree') IS NULL
    ALTER TABLE dbo.DoctorCredentialDocument ADD Degree nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.DoctorCredentialDocument', N'Specialization') IS NULL
    ALTER TABLE dbo.DoctorCredentialDocument ADD Specialization nvarchar(150) NULL;
IF COL_LENGTH(N'dbo.DoctorCredentialDocument', N'Institution') IS NULL
    ALTER TABLE dbo.DoctorCredentialDocument ADD Institution nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.DoctorCredentialDocument', N'PassingYear') IS NULL
    ALTER TABLE dbo.DoctorCredentialDocument ADD PassingYear int NULL;
GO

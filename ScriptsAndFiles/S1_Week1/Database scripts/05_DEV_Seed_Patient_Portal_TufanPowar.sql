/*
================================================================================
Author       : Tufan Powar
Created      : 17-09-2026
Script       : 05_DEV_Seed_Patient_Portal_TufanPowar.sql
Purpose      : Local/dev Patient portal login (Tufan Powar) + clinical Patient + map + doctor case.
Use          : Optional data seed. Run after 01 and 03. Login: tufanpowar001@gmail.com / 123456
Prerequisites: RoleMaster Patient; UserMaster, Patient, PatientUserMap, Doctor, CaseEntryDetails.
Idempotent   : Yes. Skips if the login already exists. Checks table/column schema before insert.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Patient', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Doctor', N'U') IS NULL
   OR OBJECT_ID(N'dbo.CaseEntryDetails', N'U') IS NULL
BEGIN
    RAISERROR('Required tables missing. Run 01 and 03 first (UserMaster/Patient/PatientUserMap/Doctor/CaseEntryDetails).', 16, 1);
    RETURN;
END

IF COL_LENGTH(N'dbo.UserMaster', N'UserName') IS NULL
   OR COL_LENGTH(N'dbo.UserMaster', N'UserPassword') IS NULL
   OR COL_LENGTH(N'dbo.UserMaster', N'RoleId') IS NULL
   OR COL_LENGTH(N'dbo.Patient', N'PatientName') IS NULL
   OR COL_LENGTH(N'dbo.PatientUserMap', N'UserId') IS NULL
   OR COL_LENGTH(N'dbo.PatientUserMap', N'PatientId') IS NULL
BEGIN
    RAISERROR('Required columns missing on UserMaster / Patient / PatientUserMap.', 16, 1);
    RETURN;
END

IF COL_LENGTH(N'dbo.Patient', N'IsWhatsAppOptIn') IS NULL
    ALTER TABLE dbo.Patient ADD IsWhatsAppOptIn BIT NOT NULL CONSTRAINT DF_Patient_IsWhatsAppOptIn_S1 DEFAULT (0);
IF COL_LENGTH(N'dbo.Patient', N'WhatsAppOptInDate') IS NULL
    ALTER TABLE dbo.Patient ADD WhatsAppOptInDate DATETIME NULL;
IF COL_LENGTH(N'dbo.Patient', N'Age') IS NULL
    ALTER TABLE dbo.Patient ADD Age INT NULL;

DECLARE @UserName NVARCHAR(200) = N'tufanpowar001@gmail.com';
DECLARE @Email NVARCHAR(200) = N'tufanpowar001@gmail.com';
DECLARE @DoctorUserName NVARCHAR(200) = N'NIGA HOMEOPATHY';
DECLARE @Pwd NVARCHAR(500) = N'PBKDF2$v1$100000$uszumO1aPL3it0x1tm/FcA==$hvFLmFwfXrC7i3Id07HFiH9ah4WiIAdjzYPyQnArLxQ=';

DECLARE @PatientRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'Patient' AND ISNULL(DeleteStatus, 0) = 0
);

IF @PatientRoleId IS NULL
BEGIN
    RAISERROR('RoleMaster missing Patient. Run 01_SEC_M01_Foundation_Security_Server.sql first.', 16, 1);
    RETURN;
END

DECLARE @DoctorId INT;
DECLARE @DoctorUserId INT;
SELECT TOP 1
    @DoctorId = d.DoctorId,
    @DoctorUserId = d.UserId
FROM dbo.Doctor d
INNER JOIN dbo.UserMaster um ON um.UserId = d.UserId
WHERE um.UserName = @DoctorUserName
  AND ISNULL(d.DeleteStatus, 0) = 0
  AND ISNULL(um.DeleteStatus, 0) = 0;

IF @DoctorId IS NULL OR @DoctorUserId IS NULL
BEGIN
    RAISERROR('Doctor login NIGA HOMEOPATHY not found. Seed cannot attach the case.', 16, 1);
    RETURN;
END

IF EXISTS (
    SELECT 1 FROM dbo.UserMaster
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND (EmailId = @Email OR UserName = @UserName)
)
BEGIN
    PRINT 'DEV seed skipped: UserMaster already has tufanpowar001@gmail.com';
    SELECT um.UserId, um.UserName, um.EmailId, um.RoleId, rm.RoleName, pum.PatientId
    FROM dbo.UserMaster um
    LEFT JOIN dbo.RoleMaster rm ON rm.RoleId = um.RoleId
    LEFT JOIN dbo.PatientUserMap pum ON pum.UserId = um.UserId AND pum.IsPrimary = 1 AND ISNULL(pum.DeleteStatus, 0) = 0
    WHERE um.UserName = @UserName OR um.EmailId = @Email;
    RETURN;
END

BEGIN TRAN;

DECLARE @UserId BIGINT;
DECLARE @PatientId INT;

INSERT INTO dbo.UserMaster (
    UserName, UserPassword, UserStatus, MobileNo, EmailId,
    FirstName, LastName, CountryId, StateId, RoleId,
    DeleteStatus, IsUserActivated, EnteredBy, EnteredDate
)
VALUES (
    @UserName,
    @Pwd,
    1,
    N'7768046064',
    @Email,
    N'Tufan',
    N'Powar',
    78,
    14,
    @PatientRoleId,
    0,
    1,
    N'S1-DEV',
    GETDATE()
);

SET @UserId = SCOPE_IDENTITY();

INSERT INTO dbo.Patient (
    PatientName, Address, StateId, CountryId, MobileNo, PhoneNo,
    DateOfBirth, Gender, Email, Age, IsWhatsAppOptIn, WhatsAppOptInDate,
    DeleteStatus, EnteredBy, EnteredDate
)
VALUES (
    N'Tufan Powar',
    N'Chambhar Lane, Vhannur, Kagal, Kolhapur, 416216',
    14,
    78,
    N'7768046064',
    NULL,
    '1998-11-15',
    0,
    @Email,
    DATEDIFF(YEAR, '1998-11-15', GETDATE()),
    1,
    GETDATE(),
    0,
    N'S1-DEV',
    GETDATE()
);

SET @PatientId = SCOPE_IDENTITY();

INSERT INTO dbo.PatientUserMap (UserId, PatientId, IsPrimary, DeleteStatus, EnteredBy, EnteredDate)
VALUES (@UserId, @PatientId, 1, 0, N'S1-DEV', GETUTCDATE());

INSERT INTO dbo.CaseEntryDetails (
    PatientId, UserId, DoctorId, DateodFirstVisit, RefBy, DeleteStatus, EnteredBy, EnteredDate
)
VALUES (
    @PatientId, @DoctorUserId, @DoctorId, GETDATE(), N'self', 0, N'S1-DEV', GETDATE()
);

COMMIT TRAN;

PRINT 'DEV seed inserted: Patient login Tufan Powar';
SELECT @UserId AS UserId, @PatientId AS PatientId, @DoctorId AS DoctorId, N'Patient' AS RoleName;
GO

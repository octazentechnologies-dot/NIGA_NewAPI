/*
================================================================================
Author       : Tufan Powar
Created      : 17-09-2026
Script       : 06_VERIFY_S1_Week1.sql
Purpose      : Read-only proof that Week 1 schema, roles, menus, and optional Patient seed exist.
Use          : Run last after 01–05. Does not insert or alter data.
Prerequisites: Scripts 01–04 (05 optional).
Idempotent   : Yes (select only).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '--- SEC-01.01 UserPassword width (expect 500) ---';
IF COL_LENGTH(N'dbo.UserMaster', N'UserPassword') IS NULL
    PRINT 'SKIP: UserMaster.UserPassword missing';
ELSE
    SELECT TABLE_NAME, COLUMN_NAME, CHARACTER_MAXIMUM_LENGTH
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'UserMaster' AND COLUMN_NAME = N'UserPassword';

PRINT '--- Foundation + family tables ---';
SELECT name AS TableName
FROM sys.tables
WHERE schema_id = SCHEMA_ID(N'dbo')
  AND name IN (
    N'PasswordResetToken', N'ConsentType', N'ConsentRecord',
    N'OtpChallenge', N'OtpAuditLog', N'AuditEvent', N'SecureDocument',
    N'PatientUserMap', N'PatientFamilyMember', N'CaregiverAuthorization',
    N'UserAppPreference', N'WelcomeSlide', N'DevicePushToken'
  )
ORDER BY name;

PRINT '--- Patient menus ---';
IF OBJECT_ID(N'dbo.MenuMaster', N'U') IS NULL
    PRINT 'SKIP: MenuMaster missing';
ELSE
    SELECT MenuId, MenuName, MenuUrl
    FROM dbo.MenuMaster
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND MenuUrl IN (N'/family', N'/caregiver');

PRINT '--- SEC-01.01 remaining plaintext passwords (UserPassword not like PBKDF2%) ---';
IF COL_LENGTH(N'dbo.UserMaster', N'UserPassword') IS NULL
    PRINT 'SKIP: UserMaster.UserPassword missing';
ELSE
    SELECT COUNT(*) AS PlainOrUnknownCount
    FROM dbo.UserMaster
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND UserPassword NOT LIKE N'PBKDF2$%';

PRINT '--- ConsentType codes (expect Privacy, Booking, TeleRecording, PharmacyShare, Marketing, Caregiver) ---';
IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NULL
    PRINT 'SKIP: ConsentType missing';
ELSE
    SELECT ConsentTypeId, Code, Name, IsActive FROM dbo.ConsentType ORDER BY Code;

PRINT '--- FND-01.02 roles ---';
IF COL_LENGTH(N'dbo.RoleMaster', N'RoleName') IS NULL
    PRINT 'SKIP: RoleMaster.RoleName missing';
ELSE
    SELECT RoleId, RoleName FROM dbo.RoleMaster
    WHERE RoleName IN (N'Patient', N'Account', N'PharmacyPartner')
      AND ISNULL(DeleteStatus, 0) = 0;

PRINT '--- ADM-B04 Account/Pharmacy menus ---';
IF OBJECT_ID(N'dbo.MenuMaster', N'U') IS NULL
    PRINT 'SKIP: MenuMaster missing';
ELSE
    SELECT MenuId, MenuName, MenuUrl, MenuIcon, SeqNo
    FROM dbo.MenuMaster
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND (MenuUrl LIKE N'/account/%' OR MenuUrl LIKE N'/pharmacy/%')
    ORDER BY SeqNo, MenuName;

PRINT '--- RoleDetails IsView counts ---';
IF OBJECT_ID(N'dbo.RoleDetails', N'U') IS NULL
    PRINT 'SKIP: RoleDetails missing';
ELSE
    SELECT r.RoleName, COUNT(*) AS ViewableMenus
    FROM dbo.RoleDetails rd
    INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
    WHERE r.RoleName IN (N'Account', N'PharmacyPartner')
      AND rd.IsView = 1
    GROUP BY r.RoleName;

PRINT '--- DEV Patient portal seed (tufanpowar001@gmail.com) ---';
IF COL_LENGTH(N'dbo.UserMaster', N'UserName') IS NULL
    PRINT 'SKIP: UserMaster.UserName missing';
ELSE
    SELECT um.UserId, um.UserName, um.EmailId, um.RoleId, rm.RoleName, pum.PatientId
    FROM dbo.UserMaster um
    LEFT JOIN dbo.RoleMaster rm ON rm.RoleId = um.RoleId
    LEFT JOIN dbo.PatientUserMap pum
        ON pum.UserId = um.UserId AND pum.IsPrimary = 1 AND ISNULL(pum.DeleteStatus, 0) = 0
    WHERE um.UserName = N'tufanpowar001@gmail.com'
      AND ISNULL(um.DeleteStatus, 0) = 0;

PRINT '06_VERIFY_S1_Week1.sql completed.';
GO

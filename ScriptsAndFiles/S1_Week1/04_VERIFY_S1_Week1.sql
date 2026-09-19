-- S1 Week 1 verify (read-only). Run after 01, 02, 03 on HomeoCentrum_*.
SET NOCOUNT ON;

PRINT '--- SEC-01.01 UserPassword width (expect 500) ---';
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
    N'PatientUserMap', N'PatientFamilyMember', N'CaregiverAuthorization'
  )
ORDER BY name;

PRINT '--- ConsentType codes (expect Privacy, Booking, TeleRecording, PharmacyShare, Marketing, Caregiver) ---';
IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NOT NULL
    SELECT ConsentTypeId, Code, Name, IsActive FROM dbo.ConsentType ORDER BY Code;

PRINT '--- FND-01.02 roles ---';
IF COL_LENGTH('dbo.RoleMaster', 'RoleName') IS NOT NULL
    SELECT RoleId, RoleName FROM dbo.RoleMaster
    WHERE RoleName IN (N'Patient', N'Account', N'PharmacyPartner')
      AND ISNULL(DeleteStatus, 0) = 0;

PRINT '--- ADM-B04 Account/Pharmacy menus ---';
IF OBJECT_ID(N'dbo.MenuMaster', N'U') IS NOT NULL
    SELECT MenuId, MenuName, MenuUrl, MenuIcon, SeqNo
    FROM dbo.MenuMaster
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND (MenuUrl LIKE N'/account/%' OR MenuUrl LIKE N'/pharmacy/%')
    ORDER BY SeqNo, MenuName;

PRINT '--- RoleDetails IsView counts ---';
IF OBJECT_ID(N'dbo.RoleDetails', N'U') IS NOT NULL
    SELECT r.RoleName, COUNT(*) AS ViewableMenus
    FROM dbo.RoleDetails rd
    INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
    WHERE r.RoleName IN (N'Account', N'PharmacyPartner')
      AND rd.IsView = 1
    GROUP BY r.RoleName;

PRINT '04_VERIFY_S1_Week1.sql completed.';
GO

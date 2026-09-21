/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 16_DEV_Tufan_Role_Logins_Verify.sql
Purpose      : Read-only proof that Tufan_* team logins exist.
Use          : HomeoCentrum_Dev. Does not insert.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '--- Tufan UserMaster ---';
SELECT r.RoleName, u.UserId, u.UserName, u.EmailId, u.IsUserActivated, u.DeleteStatus
FROM dbo.UserMaster u
LEFT JOIN dbo.RoleMaster r ON r.RoleId = u.RoleId
WHERE ISNULL(u.DeleteStatus, 0) = 0
  AND u.UserName IN (
      N'Tufan_Admin', N'Tufan_Doctore', N'Tufan_Account', N'Tufan_Pharmacy',
      N'Tufan_Patient', N'Tufan_Caregiver', N'Tufan_NoMenu'
  )
ORDER BY r.RoleName, u.UserName;

PRINT '--- Tufan Doctor ---';
SELECT d.DoctorID, d.UserId, d.FirstName, d.LastName, d.DirectoryVisible, d.VerificationStatus, d.PracticeActivated
FROM dbo.Doctor d
INNER JOIN dbo.UserMaster u ON u.UserId = d.UserId
WHERE u.UserName = N'Tufan_Doctore';

PRINT '--- Tufan Reception ---';
SELECT ReceptionStaffID, DoctorID, UserID, FullName, EmailId, DeleteStatus
FROM dbo.DoctorReceptionStaff
WHERE UserID = N'Tufan_Reception';

PRINT '--- Tufan Patient map ---';
SELECT u.UserName, m.UserId, m.PatientId, m.IsPrimary
FROM dbo.UserMaster u
INNER JOIN dbo.PatientUserMap m ON m.UserId = u.UserId AND ISNULL(m.DeleteStatus,0)=0
WHERE u.UserName = N'Tufan_Patient';

PRINT '--- Tufan Caregiver grant ---';
SELECT c.CaregiverAuthorizationId, c.PatientId, c.CaregiverUserId, c.Scope, c.RevokedAt
FROM dbo.CaregiverAuthorization c
INNER JOIN dbo.UserMaster u ON u.UserId = c.CaregiverUserId
WHERE u.UserName = N'Tufan_Caregiver' AND ISNULL(c.DeleteStatus,0)=0;

PRINT '--- RoleDetails menu counts ---';
SELECT r.RoleName, COUNT(*) AS ViewMenus
FROM dbo.RoleDetails rd
INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
WHERE rd.IsView = 1
GROUP BY r.RoleName
ORDER BY r.RoleName;

PRINT '--- Tufan users mapped menus (GetMenuByRole source) ---';
SELECT u.UserName, r.RoleName, COUNT(rd.MenuId) AS ViewMenus
FROM dbo.UserMaster u
INNER JOIN dbo.RoleMaster r ON r.RoleId = u.RoleId
LEFT JOIN dbo.RoleDetails rd ON rd.RoleId = r.RoleId AND rd.IsView = 1
WHERE ISNULL(u.DeleteStatus, 0) = 0
  AND u.UserName IN (
      N'Tufan_Admin', N'Tufan_Doctore', N'Tufan_Account', N'Tufan_Pharmacy',
      N'Tufan_Patient', N'Tufan_Caregiver', N'Tufan_NoMenu'
  )
GROUP BY u.UserName, r.RoleName
ORDER BY r.RoleName, u.UserName;

PRINT '--- Reception role menus (JWT Role=Reception, not UserMaster) ---';
SELECT m.MenuName, m.MenuUrl
FROM dbo.RoleDetails rd
INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
INNER JOIN dbo.MenuMaster m ON m.MenuId = rd.MenuId
WHERE r.RoleName = N'Reception' AND rd.IsView = 1 AND ISNULL(m.DeleteStatus,0)=0
ORDER BY m.SeqNo;

DECLARE @Missing INT = 0;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Admin' AND ISNULL(DeleteStatus,0)=0) SET @Missing += 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Doctore' AND ISNULL(DeleteStatus,0)=0) SET @Missing += 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Account' AND ISNULL(DeleteStatus,0)=0) SET @Missing += 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Pharmacy' AND ISNULL(DeleteStatus,0)=0) SET @Missing += 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Patient' AND ISNULL(DeleteStatus,0)=0) SET @Missing += 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Caregiver' AND ISNULL(DeleteStatus,0)=0) SET @Missing += 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_NoMenu' AND ISNULL(DeleteStatus,0)=0) SET @Missing += 1;
IF NOT EXISTS (SELECT 1 FROM dbo.DoctorReceptionStaff WHERE UserID = N'Tufan_Reception' AND ISNULL(DeleteStatus,0)=0) SET @Missing += 1;
IF NOT EXISTS (
    SELECT 1 FROM dbo.Doctor d
    INNER JOIN dbo.UserMaster u ON u.UserId = d.UserId
    WHERE u.UserName = N'Tufan_Doctore' AND ISNULL(d.DeleteStatus,0)=0
) SET @Missing += 1;

IF @Missing > 0
    RAISERROR('Tufan team login(s) missing. Re-run 15_DEV_Seed_Tufan_Role_Logins.sql.', 16, 1);
ELSE
    PRINT '16_DEV_Tufan_Role_Logins_Verify OK.';
GO

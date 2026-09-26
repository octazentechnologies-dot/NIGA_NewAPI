/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Updated      : 23-09-2026
Script       : 14_DEV_Dashboard_Users_Verify.sql
Purpose      : Read-only proof that Tufan_* Dev logins exist for every web
               dashboard delivered in S1 Week 1 + S2 Week 2.
Use          : HomeoCentrum_Dev only. Does not insert or update.
Logins       : Tufan_Admin / Tufan_Doctor / Tufan_Account / Tufan_Pharmacy /
               Tufan_Patient / Tufan_Caregiver / Tufan_NoMenu
               Reception staff: Tufan_Reception
               Email tufanpowar001@gmail.com  mobile 7768046064  password 123456
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '--- RoleMaster ---';
SELECT RoleId, RoleName, DeleteStatus
FROM dbo.RoleMaster
WHERE ISNULL(DeleteStatus, 0) = 0
ORDER BY RoleId;

PRINT '--- Tufan_* UserMaster dashboard logins ---';
SELECT r.RoleName, u.UserId, u.UserName, u.EmailId, u.MobileNo, u.IsUserActivated, u.DeleteStatus
FROM dbo.UserMaster u
LEFT JOIN dbo.RoleMaster r ON r.RoleId = u.RoleId
WHERE ISNULL(u.DeleteStatus, 0) = 0
  AND u.UserName IN (
      N'Tufan_Admin',
      N'Tufan_Doctor',
      N'Tufan_Account',
      N'Tufan_Pharmacy',
      N'Tufan_Patient',
      N'Tufan_Caregiver',
      N'Tufan_NoMenu',
      N'tufanpowar001@gmail.com'
  )
ORDER BY r.RoleName, u.UserName;

PRINT '--- Reception staff login (not UserMaster) ---';
SELECT ReceptionStaffID, DoctorID, UserID, FullName, EmailId, ContactNumber, DeleteStatus
FROM dbo.DoctorReceptionStaff
WHERE UserID = N'Tufan_Reception' AND ISNULL(DeleteStatus, 0) = 0;

PRINT '--- RoleDetails menu counts ---';
SELECT r.RoleName, COUNT(*) AS MenuRows
FROM dbo.RoleDetails rd
INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
GROUP BY r.RoleName
ORDER BY r.RoleName;

DECLARE @Missing INT = 0;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Admin' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Account' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Pharmacy' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Patient' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.DoctorReceptionStaff WHERE UserID = N'Tufan_Reception' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;

IF @Missing > 0
    RAISERROR('Tufan dashboard login(s) missing. Re-run 15_DEV_Seed_Tufan_Role_Logins.sql.', 16, 1);
ELSE
    PRINT '14_DEV_Dashboard_Users_Verify OK — Tufan_* Dev dashboard logins present.';
GO

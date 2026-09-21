/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 14_DEV_Dashboard_Users_Verify.sql
Purpose      : Read-only proof that Dev logins exist for every web dashboard
               delivered in S1 Week 1 + S2 Week 2.
Use          : HomeoCentrum_Dev only. Does not insert or update.
Logins       : admin / niga homeopathy / testdoctor / s2.reception /
               s2.account / s2.pharmacy / tufanpowar001@gmail.com /
               s1.caregiver.tested@homeocentrum.dev
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

PRINT '--- UserMaster dashboard logins ---';
SELECT r.RoleName, u.UserId, u.UserName, u.EmailId, u.IsUserActivated, u.DeleteStatus
FROM dbo.UserMaster u
LEFT JOIN dbo.RoleMaster r ON r.RoleId = u.RoleId
WHERE ISNULL(u.DeleteStatus, 0) = 0
  AND u.UserName IN (
      N'admin',
      N'niga homeopathy',
      N'testdoctor',
      N's2.account',
      N's2.pharmacy',
      N'tufanpowar001@gmail.com',
      N's1.caregiver.tested@homeocentrum.dev'
  )
ORDER BY r.RoleName, u.UserName;

PRINT '--- Reception staff login (not UserMaster) ---';
SELECT ReceptionStaffID, DoctorID, UserID, FullName, EmailId, DeleteStatus
FROM dbo.DoctorReceptionStaff
WHERE UserID = N's2.reception';

PRINT '--- RoleDetails menu counts ---';
SELECT r.RoleName, COUNT(*) AS MenuRows
FROM dbo.RoleDetails rd
INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
GROUP BY r.RoleName
ORDER BY r.RoleName;

DECLARE @Missing INT = 0;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'admin' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'niga homeopathy' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'testdoctor' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N's2.account' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N's2.pharmacy' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'tufanpowar001@gmail.com' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;
IF NOT EXISTS (SELECT 1 FROM dbo.DoctorReceptionStaff WHERE UserID = N's2.reception' AND ISNULL(DeleteStatus,0)=0)
    SET @Missing = @Missing + 1;

IF @Missing > 0
    RAISERROR('Dashboard login(s) missing. Re-run S1 05 seed, S2 08 reception, S2 12 role users.', 16, 1);
ELSE
    PRINT '14_DEV_Dashboard_Users_Verify OK — all expected Dev dashboard logins present.';

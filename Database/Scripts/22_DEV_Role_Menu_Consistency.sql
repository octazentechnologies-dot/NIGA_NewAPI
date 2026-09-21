/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 22_DEV_Role_Menu_Consistency.sql
Purpose      : Read-only proof that New User → Role → RoleDetails → MenuMaster
               is internally consistent after scripts 15 and 19.
Use          : HomeoCentrum_Dev. Does not insert. Run after 15 + 19.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '--- Duplicate RoleDetails (RoleId, MenuId) ---';
SELECT rd.RoleId, r.RoleName, rd.MenuId, COUNT(*) AS DupCount
FROM dbo.RoleDetails rd
INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
GROUP BY rd.RoleId, r.RoleName, rd.MenuId
HAVING COUNT(*) > 1;

PRINT '--- RoleDetails pointing at deleted/hidden menus ---';
SELECT r.RoleName, m.MenuId, m.MenuName, m.MenuUrl, m.DeleteStatus, m.ShowInMainMenu
FROM dbo.RoleDetails rd
INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
INNER JOIN dbo.MenuMaster m ON m.MenuId = rd.MenuId
WHERE rd.IsView = 1
  AND (ISNULL(m.DeleteStatus, 0) = 1 OR ISNULL(m.ShowInMainMenu, 1) = 0);

PRINT '--- Expected live web roles ---';
SELECT r.RoleName,
       SUM(CASE WHEN rd.IsView = 1 THEN 1 ELSE 0 END) AS ViewMenus
FROM dbo.RoleMaster r
LEFT JOIN dbo.RoleDetails rd ON rd.RoleId = r.RoleId
WHERE r.RoleName IN (N'Admin', N'Doctor', N'Reception', N'Patient', N'Account', N'PharmacyPartner', N'EmptyMenuProbe')
  AND ISNULL(r.DeleteStatus, 0) = 0
GROUP BY r.RoleName
ORDER BY r.RoleName;

PRINT '--- Tufan users: role, active, mapped menu count ---';
SELECT u.UserName, r.RoleName, u.IsUserActivated, u.UserStatus, u.DeleteStatus,
       COUNT(rd.MenuId) AS ViewMenus
FROM dbo.UserMaster u
LEFT JOIN dbo.RoleMaster r ON r.RoleId = u.RoleId
LEFT JOIN dbo.RoleDetails rd ON rd.RoleId = r.RoleId AND rd.IsView = 1
WHERE ISNULL(u.DeleteStatus, 0) = 0
  AND u.UserName IN (
      N'Tufan_Admin', N'Tufan_Doctore', N'Tufan_Account', N'Tufan_Pharmacy',
      N'Tufan_Patient', N'Tufan_Caregiver', N'Tufan_NoMenu'
  )
GROUP BY u.UserName, r.RoleName, u.IsUserActivated, u.UserStatus, u.DeleteStatus
ORDER BY u.UserName;

PRINT '--- Reception staff linked to Tufan_Doctore ---';
SELECT s.UserID, s.DoctorID, d.UserId AS DoctorUserId, u.UserName AS DoctorUserName
FROM dbo.DoctorReceptionStaff s
LEFT JOIN dbo.Doctor d ON d.DoctorID = s.DoctorID
LEFT JOIN dbo.UserMaster u ON u.UserId = d.UserId
WHERE s.UserID = N'Tufan_Reception' AND ISNULL(s.DeleteStatus, 0) = 0;

DECLARE @Fail INT = 0;

IF EXISTS (
    SELECT 1 FROM dbo.RoleDetails
    GROUP BY RoleId, MenuId
    HAVING COUNT(*) > 1
) SET @Fail += 1;

IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_NoMenu' AND ISNULL(DeleteStatus,0)=0)
    SET @Fail += 1;

IF EXISTS (
    SELECT 1
    FROM dbo.UserMaster u
    INNER JOIN dbo.RoleMaster r ON r.RoleId = u.RoleId
    INNER JOIN dbo.RoleDetails rd ON rd.RoleId = r.RoleId AND rd.IsView = 1
    WHERE u.UserName = N'Tufan_NoMenu'
)
    SET @Fail += 1;

IF EXISTS (
    SELECT 1
    FROM dbo.RoleDetails rd
    INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
    INNER JOIN dbo.MenuMaster m ON m.MenuId = rd.MenuId
    WHERE r.RoleName = N'Doctor' AND rd.IsView = 1
      AND m.MenuUrl IN (N'/enquiries', N'/admin/enquiries')
) SET @Fail += 1;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.RoleDetails rd
    INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
    INNER JOIN dbo.MenuMaster m ON m.MenuId = rd.MenuId
    WHERE r.RoleName = N'Reception' AND rd.IsView = 1 AND m.MenuUrl = N'/doctordashboard'
) SET @Fail += 1;

IF EXISTS (
    SELECT 1
    FROM dbo.RoleDetails rd
    INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
    INNER JOIN dbo.MenuMaster m ON m.MenuId = rd.MenuId
    WHERE r.RoleName = N'Reception' AND rd.IsView = 1 AND m.MenuUrl = N'/doctor/patientboard'
) SET @Fail += 1;

IF @Fail > 0
    RAISERROR('22_DEV_Role_Menu_Consistency FAILED. Re-run 15 then 19.', 16, 1);
ELSE
    PRINT '22_DEV_Role_Menu_Consistency OK.';
GO

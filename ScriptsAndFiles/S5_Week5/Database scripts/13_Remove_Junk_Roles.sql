/*
  13 - Remove junk roles from RoleMaster.

    RoleName        Placeholder row (literally named "RoleName"); no users, no menus.
    EmptyMenuProbe  Dev test role from S2_Week2/15 (BUG-S1-01 empty-menu probe) and its only login Tufan_NoMenu.

  Roles are matched by name, not id. A role is removed only when no other user still has it; otherwise the
  script prints the user count and leaves that role alone. RoleDetails (menu rights) rows for the role go too.
  AuditEvent history for Tufan_NoMenu is kept. Safe to run more than once.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DELETE FROM dbo.UserMaster
WHERE UserName = N'Tufan_NoMenu'
  AND RoleId IN (SELECT RoleId FROM dbo.RoleMaster WHERE RoleName = N'EmptyMenuProbe');
IF @@ROWCOUNT > 0 PRINT 'Deleted login Tufan_NoMenu';

DECLARE @Junk TABLE (RoleId INT PRIMARY KEY, RoleName NVARCHAR(100), Users INT);
INSERT INTO @Junk (RoleId, RoleName, Users)
SELECT r.RoleId, r.RoleName, (SELECT COUNT(*) FROM dbo.UserMaster u WHERE u.RoleId = r.RoleId)
FROM dbo.RoleMaster r
WHERE r.RoleName IN (N'RoleName', N'EmptyMenuProbe');

SELECT RoleId, RoleName, Users,
       CASE WHEN Users = 0 THEN 'Deleted' ELSE 'Kept: users still have this role' END AS Result
FROM @Junk;

DELETE d FROM dbo.RoleDetails d JOIN @Junk j ON j.RoleId = d.RoleId WHERE j.Users = 0;
DELETE r FROM dbo.RoleMaster r JOIN @Junk j ON j.RoleId = r.RoleId WHERE j.Users = 0;

COMMIT;

PRINT '13_Remove_Junk_Roles: done';

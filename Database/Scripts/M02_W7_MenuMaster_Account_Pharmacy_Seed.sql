-- M02 W7 / ADM-B04.01 — Optional MenuMaster seed for future Account / Pharmacy roles.
-- Do NOT run blindly: only when Account or Pharmacy modules are enabled and roles exist.
-- Existing Admin clinical menus stay as-is (no schema change required for M02 Admin Clinical).
--
-- Prerequisites:
--   1. ModuleMaster rows for Account / Pharmacy (adjust ModuleId).
--   2. RoleMaster rows for Account / Pharmacy roles (adjust RoleId).
--   3. Review FirmIds / SeqNo for your environment.

/*
DECLARE @AccountModuleId INT = /* TODO */;
DECLARE @PharmacyModuleId INT = /* TODO */;
DECLARE @AccountRoleId INT = /* TODO */;
DECLARE @PharmacyRoleId INT = /* TODO */;

-- Example menu rows (customize URLs to match future UI routes)
-- INSERT INTO MenuMaster (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description, MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds, EnteredBy, EnteredDate, DeleteStatus)
-- VALUES
-- (@AccountModuleId, N'Account Dashboard', N'', N'Menu', NULL, N'/account/dashboard', N'Account module', N'ri-bank-line', NULL, NULL, 1, 1, 100, N'0', N'system', GETUTCDATE(), 0),
-- (@PharmacyModuleId, N'Pharmacy Dashboard', N'', N'Menu', NULL, N'/pharmacy/dashboard', N'Pharmacy module', N'ri-capsule-line', NULL, NULL, 1, 1, 200, N'0', N'system', GETUTCDATE(), 0);

-- Then link RoleDetails (IsView/IsAdd/IsModify/IsDelete) for those MenuId values to @AccountRoleId / @PharmacyRoleId.
*/

-- Verification after seed:
-- SELECT m.MenuId, m.MenuName, m.MenuUrl, rd.RoleId, rd.IsView
-- FROM MenuMaster m
-- JOIN RoleDetails rd ON rd.MenuId = m.MenuId
-- WHERE m.DeleteStatus = 0 AND m.MenuUrl LIKE '/%account%' OR m.MenuUrl LIKE '/%pharmacy%';

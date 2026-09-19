-- M02 W7 / ADM-B04.02 + SEC-04.03 — Account & Pharmacy MenuMaster + RoleDetails seed.
-- Idempotent. Requires RoleMaster rows for Account / PharmacyPartner (M01_Foundation_Security_Server.sql).
-- Does NOT alter existing Admin clinical menus.
-- SPA still uses hard-coded LayoutMenuData until ADM-B04.03 (UI track).

SET NOCOUNT ON;

DECLARE @AccountRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'Account' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @PharmacyRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'PharmacyPartner' AND ISNULL(DeleteStatus, 0) = 0
);

IF @AccountRoleId IS NULL OR @PharmacyRoleId IS NULL
BEGIN
    RAISERROR('RoleMaster missing Account and/or PharmacyPartner. Run M01_Foundation_Security_Server.sql first.', 16, 1);
    RETURN;
END

-- ---------------------------------------------------------------------------
-- ModuleMaster
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.ModuleMaster WHERE ModuleName = N'Account' AND ISNULL(DeleteStatus, 0) = 0)
BEGIN
    INSERT INTO dbo.ModuleMaster
        (ModuleName, ModuleMarathiName, ModuleIcon, ModuleAreaName, Seqno, IsDirectNode,
         ActionName, ControllerName, ModuleUrl, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (N'Account', N'', N'ri-bank-line', N'Account', 900, 1,
         NULL, NULL, N'/account/home', N'M02', GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.ModuleMaster WHERE ModuleName = N'Pharmacy' AND ISNULL(DeleteStatus, 0) = 0)
BEGIN
    INSERT INTO dbo.ModuleMaster
        (ModuleName, ModuleMarathiName, ModuleIcon, ModuleAreaName, Seqno, IsDirectNode,
         ActionName, ControllerName, ModuleUrl, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (N'Pharmacy', N'', N'ri-capsule-line', N'Pharmacy', 910, 1,
         NULL, NULL, N'/pharmacy/home', N'M02', GETUTCDATE(), 0);
END

DECLARE @AccountModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Account' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @PharmacyModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Pharmacy' AND ISNULL(DeleteStatus, 0) = 0
);

-- ---------------------------------------------------------------------------
-- Helper: ensure menu + RoleDetails IsView
-- ---------------------------------------------------------------------------
-- Account menus (FND-02.01 stubs)
;WITH AccountMenus(MenuName, MenuUrl, SeqNo, Icon) AS (
    SELECT * FROM (VALUES
        (N'Account Home',     N'/account/home',     100, N'ri-home-line'),
        (N'Ledger',           N'/account/ledger',   110, N'ri-book-line'),
        (N'Doctor Earnings',  N'/account/earnings', 120, N'ri-money-dollar-circle-line'),
        (N'Payouts',          N'/account/payouts',  130, N'ri-bank-card-line'),
        (N'Invoices',         N'/account/invoices', 140, N'ri-file-list-3-line'),
        (N'Reports',          N'/account/reports',  150, N'ri-bar-chart-line')
    ) v(MenuName, MenuUrl, SeqNo, Icon)
)
SELECT * INTO #AccountMenus FROM AccountMenus;

DECLARE @MenuName NVARCHAR(200), @MenuUrl NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100);
DECLARE @MenuId INT;

DECLARE account_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MenuName, MenuUrl, SeqNo, Icon FROM #AccountMenus;
OPEN account_cursor;
FETCH NEXT FROM account_cursor INTO @MenuName, @MenuUrl, @SeqNo, @Icon;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0)
    BEGIN
        INSERT INTO dbo.MenuMaster
            (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
             MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
             EnteredBy, EnteredDate, DeleteStatus)
        VALUES
            (@AccountModuleId, @MenuName, N'', N'Menu', NULL, @MenuUrl, N'Account stub (M08 screens later)',
             @Icon, NULL, NULL, 1, 1, @SeqNo, N'0', N'M02', GETUTCDATE(), 0);
    END

    SELECT @MenuId = MenuId FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0;

    IF @MenuId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @AccountRoleId AND MenuId = @MenuId)
    BEGIN
        INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
        VALUES (@AccountRoleId, @MenuId, 1, 0, 0, 0);
    END

    -- SEC-04.03: do not grant Account menus to Admin via this seed (Admin keeps clinical menus only).

    FETCH NEXT FROM account_cursor INTO @MenuName, @MenuUrl, @SeqNo, @Icon;
END
CLOSE account_cursor;
DEALLOCATE account_cursor;
DROP TABLE #AccountMenus;

-- Pharmacy stub
IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl = N'/pharmacy/home' AND ISNULL(DeleteStatus, 0) = 0)
BEGIN
    INSERT INTO dbo.MenuMaster
        (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
         MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
         EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (@PharmacyModuleId, N'Pharmacy Home', N'', N'Menu', NULL, N'/pharmacy/home', N'HomeoMeds stub',
         N'ri-capsule-line', NULL, NULL, 1, 1, 200, N'0', N'M02', GETUTCDATE(), 0);
END

SELECT @MenuId = MenuId FROM dbo.MenuMaster WHERE MenuUrl = N'/pharmacy/home' AND ISNULL(DeleteStatus, 0) = 0;
IF @MenuId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @PharmacyRoleId AND MenuId = @MenuId)
BEGIN
    INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
    VALUES (@PharmacyRoleId, @MenuId, 1, 0, 0, 0);
END

PRINT 'M02_W7_MenuMaster_Account_Pharmacy_Seed.sql completed.';
PRINT 'Verify: GET /api/mastersAPI/GetMenuByRole for an Account userId.';

-- Verification helper (read-only):
-- SELECT m.MenuId, m.MenuName, m.MenuUrl, rd.RoleId, r.RoleName, rd.IsView
-- FROM dbo.MenuMaster m
-- JOIN dbo.RoleDetails rd ON rd.MenuId = m.MenuId
-- JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
-- WHERE m.DeleteStatus = 0
--   AND (m.MenuUrl LIKE N'/account%' OR m.MenuUrl LIKE N'/pharmacy%');
GO

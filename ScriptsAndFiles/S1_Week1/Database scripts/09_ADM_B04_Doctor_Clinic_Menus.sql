/*
================================================================================
Author       : Tufan Powar
Created      : 20-09-2026
Script       : 09_ADM_B04_Doctor_Clinic_Menus.sql
Purpose      : Seed Clinic module + doctor SPA menus and RoleDetails (IsView)
               so GET /api/mastersAPI/GetMenuByRole returns 200 for Doctor.
               Enquiries is Admin-only (see S2 19_DEV_Seed_Role_Menus.sql).
Use          : Run on localhost HomeoCentrum_Dev after script 02 (Account/Pharmacy).
Prerequisites: RoleMaster Doctor; ModuleMaster, MenuMaster, RoleDetails.
Idempotent   : Yes. Inserts module/menu/role-detail rows only when missing.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.RoleMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ModuleMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.MenuMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RoleDetails', N'U') IS NULL
BEGIN
    RAISERROR('Required tables missing (RoleMaster / ModuleMaster / MenuMaster / RoleDetails).', 16, 1);
    RETURN;
END

IF COL_LENGTH(N'dbo.RoleMaster', N'RoleName') IS NULL
   OR COL_LENGTH(N'dbo.MenuMaster', N'MenuUrl') IS NULL
   OR COL_LENGTH(N'dbo.RoleDetails', N'IsView') IS NULL
BEGIN
    RAISERROR('Required columns missing on RoleMaster / MenuMaster / RoleDetails.', 16, 1);
    RETURN;
END

DECLARE @DoctorRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'Doctor' AND ISNULL(DeleteStatus, 0) = 0
);

IF @DoctorRoleId IS NULL
BEGIN
    RAISERROR('RoleMaster missing Doctor.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.ModuleMaster WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus, 0) = 0)
BEGIN
    INSERT INTO dbo.ModuleMaster
        (ModuleName, ModuleMarathiName, ModuleIcon, ModuleAreaName, Seqno, IsDirectNode,
         ActionName, ControllerName, ModuleUrl, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (N'Clinic', N'', N'ri-stethoscope-line', N'Clinic', 800, 1,
         NULL, NULL, N'/doctordashboard', N'S1', GETUTCDATE(), 0);
END

DECLARE @ClinicModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus, 0) = 0
);

IF @ClinicModuleId IS NULL
BEGIN
    RAISERROR('Clinic ModuleMaster row could not be resolved.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#DoctorMenus') IS NOT NULL DROP TABLE #DoctorMenus;
SELECT * INTO #DoctorMenus FROM (VALUES
    (N'Dashboard',     N'/doctordashboard',      100, N'ri-dashboard-2-line'),
    (N'Patient Board', N'/doctor/patientboard',  110, N'ri-user-heart-line'),
    (N'Anatomy',       N'/doctor/anatomy',       120, N'ri-body-scan-line')
) v(MenuName, MenuUrl, SeqNo, Icon);

DECLARE @MenuName NVARCHAR(200), @MenuUrl NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100);
DECLARE @MenuId INT;

DECLARE doctor_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MenuName, MenuUrl, SeqNo, Icon FROM #DoctorMenus;
OPEN doctor_cursor;
FETCH NEXT FROM doctor_cursor INTO @MenuName, @MenuUrl, @SeqNo, @Icon;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0)
    BEGIN
        INSERT INTO dbo.MenuMaster
            (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
             MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
             EnteredBy, EnteredDate, DeleteStatus)
        VALUES
            (@ClinicModuleId, @MenuName, N'', N'Menu', NULL, @MenuUrl, N'Clinic doctor SPA (ADM-B04)',
             @Icon, NULL, NULL, 1, 1, @SeqNo, N'0', N'S1', GETUTCDATE(), 0);
    END

    SELECT @MenuId = MenuId FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0;

    IF @MenuId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @DoctorRoleId AND MenuId = @MenuId)
    BEGIN
        INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
        VALUES (@DoctorRoleId, @MenuId, 1, 0, 0, 0);
    END

    FETCH NEXT FROM doctor_cursor INTO @MenuName, @MenuUrl, @SeqNo, @Icon;
END
CLOSE doctor_cursor;
DEALLOCATE doctor_cursor;
DROP TABLE #DoctorMenus;

PRINT 'Doctor RoleDetails mapped. Verify: GET /api/mastersAPI/GetMenuByRole for a Doctor userId.';
GO

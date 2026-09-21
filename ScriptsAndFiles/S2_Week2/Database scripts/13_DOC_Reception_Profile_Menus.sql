/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 13_DOC_Reception_Profile_Menus.sql
Purpose      : Doctor SPA menus for Reception Staff and Profile (DOC-09 / DOC-10).
Use          : HomeoCentrum_Dev after 09 (or after Clinic module exists).
Idempotent   : Yes.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @DoctorRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'Doctor' AND ISNULL(DeleteStatus, 0) = 0
);

DECLARE @ClinicModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster
    WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus, 0) = 0
);

IF @DoctorRoleId IS NULL OR @ClinicModuleId IS NULL
BEGIN
    RAISERROR('Clinic module or Doctor role missing. Run S1 09_ADM_B04_Doctor_Clinic_Menus.sql first.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#ExtraDoctorMenus') IS NOT NULL DROP TABLE #ExtraDoctorMenus;
SELECT * INTO #ExtraDoctorMenus FROM (VALUES
    (N'Reception Staff', N'/doctor/reception-staff', 140, N'ri-user-star-line'),
    (N'Profile',         N'/profile',                150, N'ri-user-settings-line')
) v(MenuName, MenuUrl, SeqNo, Icon);

DECLARE @MenuName NVARCHAR(200), @MenuUrl NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100);
DECLARE @MenuId INT;

DECLARE extra_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MenuName, MenuUrl, SeqNo, Icon FROM #ExtraDoctorMenus;
OPEN extra_cursor;
FETCH NEXT FROM extra_cursor INTO @MenuName, @MenuUrl, @SeqNo, @Icon;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0)
    BEGIN
        INSERT INTO dbo.MenuMaster
            (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
             MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
             EnteredBy, EnteredDate, DeleteStatus)
        VALUES
            (@ClinicModuleId, @MenuName, N'', N'Menu', NULL, @MenuUrl, N'S2 Week 2 doctor SPA',
             @Icon, NULL, NULL, 1, 1, @SeqNo, N'0', N'S2', GETUTCDATE(), 0);
    END

    SELECT @MenuId = MenuId FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0;

    IF @MenuId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @DoctorRoleId AND MenuId = @MenuId)
    BEGIN
        INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
        VALUES (@DoctorRoleId, @MenuId, 1, 0, 0, 0);
    END

    FETCH NEXT FROM extra_cursor INTO @MenuName, @MenuUrl, @SeqNo, @Icon;
END
CLOSE extra_cursor;
DEALLOCATE extra_cursor;
DROP TABLE #ExtraDoctorMenus;

PRINT 'Doctor reception-staff and profile menus mapped.';
GO

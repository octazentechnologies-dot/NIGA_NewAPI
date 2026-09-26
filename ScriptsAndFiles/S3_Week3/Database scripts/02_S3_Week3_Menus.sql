/*
================================================================================
Script : 02_S3_Week3_Menus.sql
Purpose: Clinic menus for schedule, reception home, support.
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @ClinicModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster
    WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @AdminModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster
    WHERE ModuleName IN (N'Admin', N'Administration', N'Masters') AND ISNULL(DeleteStatus, 0) = 0
    ORDER BY CASE ModuleName WHEN N'Admin' THEN 0 ELSE 1 END
);
DECLARE @DoctorRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Doctor' AND ISNULL(DeleteStatus, 0) = 0);
DECLARE @ReceptionRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Reception' AND ISNULL(DeleteStatus, 0) = 0);
DECLARE @AdminRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Admin' AND ISNULL(DeleteStatus, 0) = 0);

IF @ClinicModuleId IS NULL OR @DoctorRoleId IS NULL
BEGIN
    RAISERROR('Clinic module or Doctor role missing.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#S3Menus') IS NOT NULL DROP TABLE #S3Menus;
SELECT * INTO #S3Menus FROM (VALUES
    (@ClinicModuleId, N'Schedule',        N'/doctor/schedule',         160, N'ri-calendar-2-line',    @DoctorRoleId),
    (@ClinicModuleId, N'Support',         N'/doctor/support',          170, N'ri-customer-service-2-line', @DoctorRoleId),
    (@ClinicModuleId, N'Teleconsult',     N'/doctor/tele',             180, N'ri-vidicon-line',       @DoctorRoleId),
    (@ClinicModuleId, N'Reception',       N'/reception',               10,  N'ri-dashboard-2-line',   @ReceptionRoleId),
    (@ClinicModuleId, N'Schedule',        N'/reception/schedule',      20,  N'ri-calendar-2-line',    @ReceptionRoleId),
    (@ClinicModuleId, N'Case paper',      N'/reception/case-paper',    30,  N'ri-file-list-3-line',   @ReceptionRoleId),
    (@ClinicModuleId, N'Profile',         N'/profile',                 40,  N'ri-user-settings-line', @ReceptionRoleId),
    (ISNULL(@AdminModuleId, @ClinicModuleId), N'Support tickets', N'/admin/support-tickets', 900, N'ri-ticket-2-line', @AdminRoleId)
) v(ModuleId, MenuName, MenuUrl, SeqNo, Icon, RoleId);

DECLARE @ModuleId INT, @MenuName NVARCHAR(200), @MenuUrl NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100), @RoleId INT, @MenuId INT;
DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT ModuleId, MenuName, MenuUrl, SeqNo, Icon, RoleId FROM #S3Menus WHERE RoleId IS NOT NULL;
OPEN c;
FETCH NEXT FROM c INTO @ModuleId, @MenuName, @MenuUrl, @SeqNo, @Icon, @RoleId;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0)
    BEGIN
        INSERT INTO dbo.MenuMaster
            (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
             MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
             EnteredBy, EnteredDate, DeleteStatus)
        VALUES
            (@ModuleId, @MenuName, N'', N'Menu', NULL, @MenuUrl, N'S3 Week 3',
             @Icon, NULL, NULL, 1, 1, @SeqNo, N'0', N'S3', GETUTCDATE(), 0);
    END

    SELECT @MenuId = MenuId FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0;
    IF @MenuId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @RoleId AND MenuId = @MenuId)
    BEGIN
        INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
        VALUES (@RoleId, @MenuId, 1, 0, 0, 0);
    END

    FETCH NEXT FROM c INTO @ModuleId, @MenuName, @MenuUrl, @SeqNo, @Icon, @RoleId;
END
CLOSE c;
DEALLOCATE c;
DROP TABLE #S3Menus;
PRINT 'S3 menus mapped.';
GO

/*
S5 Week 5 — step 2 of 3. Run after 01_S5_Week5_Schema.sql and before 03_S5_Week5_Demo_Data.sql.
Admin clinic reports, follow-ups, and notification menus (idempotent).
*/
SET NOCOUNT ON;
GO

DECLARE @DoctorRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Doctor' AND ISNULL(DeleteStatus,0)=0);
DECLARE @AdminRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Admin' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PatientRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Patient' AND ISNULL(DeleteStatus,0)=0);
DECLARE @ClinicModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus,0)=0);
DECLARE @AdminModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'AdminPortal' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PatientModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'PatientApp' AND ISNULL(DeleteStatus,0)=0);

IF OBJECT_ID('tempdb..#S5Menus') IS NOT NULL DROP TABLE #S5Menus;
SELECT * INTO #S5Menus FROM (VALUES
    (N'Admin', N'AdminPortal', N'Clinic reports', N'/admin/reports', 59, N'ri-bar-chart-box-line'),
    (N'Patient', N'PatientApp', N'Notifications', N'/patient/notifications', 140, N'ri-notification-3-line'),
    (N'Doctor', N'Clinic', N'Follow-ups due', N'/doctor/follow-ups', 129, N'ri-calendar-check-line')
) v(RoleName, ModuleName, MenuName, MenuUrl, SeqNo, Icon);

DECLARE @RoleName NVARCHAR(50), @ModuleName NVARCHAR(100), @MenuName NVARCHAR(200),
        @MenuUrl NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100);
DECLARE @RoleId INT, @ModuleId INT, @MenuId INT;

DECLARE s5_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName, ModuleName, MenuName, MenuUrl, SeqNo, Icon FROM #S5Menus ORDER BY SeqNo;
OPEN s5_cursor;
FETCH NEXT FROM s5_cursor INTO @RoleName, @ModuleName, @MenuName, @MenuUrl, @SeqNo, @Icon;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @RoleId = CASE @RoleName
        WHEN N'Doctor' THEN @DoctorRoleId
        WHEN N'Admin' THEN @AdminRoleId
        WHEN N'Patient' THEN @PatientRoleId
        ELSE NULL END;
    SET @ModuleId = CASE @ModuleName
        WHEN N'Clinic' THEN @ClinicModuleId
        WHEN N'AdminPortal' THEN @AdminModuleId
        WHEN N'PatientApp' THEN @PatientModuleId
        ELSE NULL END;

    SET @MenuId = (
        SELECT TOP 1 MenuId FROM dbo.MenuMaster
        WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0
        ORDER BY MenuId
    );

    IF @MenuId IS NULL AND @ModuleId IS NOT NULL
    BEGIN
        INSERT INTO dbo.MenuMaster
            (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
             MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
             EnteredBy, EnteredDate, DeleteStatus)
        VALUES
            (@ModuleId, @MenuName, N'', N'Menu', NULL, @MenuUrl, N'S5 Week 5 menu',
             @Icon, NULL, NULL, 1, 1, @SeqNo, N'0', N'S5-W5', GETUTCDATE(), 0);
        SET @MenuId = SCOPE_IDENTITY();
    END

    IF @MenuId IS NOT NULL AND @RoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @RoleId AND MenuId = @MenuId)
        INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
        VALUES (@RoleId, @MenuId, 1, 0, 0, 0);

    FETCH NEXT FROM s5_cursor INTO @RoleName, @ModuleName, @MenuName, @MenuUrl, @SeqNo, @Icon;
END
CLOSE s5_cursor;
DEALLOCATE s5_cursor;
DROP TABLE #S5Menus;
PRINT '02_S5_Week5_Menus.sql completed.';
GO

/*
================================================================================
Script       : 010_Seed_Appointment_Requests_Menus.sql
Purpose      : Add Appointment requests menus + RoleDetails so GetMenuByRole
               returns a top-nav icon for Doctor and Patient.
Routes       : /doctor/appointment-requests  (ri-mail-check-line)
               /patient/appointment-requests (ri-mail-send-line)
Idempotent   : Yes — match by MenuUrl / RoleId+MenuId.
Safe for     : Dev / Stage / Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.RoleMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ModuleMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.MenuMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RoleDetails', N'U') IS NULL
BEGIN
    RAISERROR('Required tables missing.', 16, 1);
    RETURN;
END

DECLARE @DoctorRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'Doctor' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @PatientRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'Patient' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @ClinicModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster
    WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @PatientModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster
    WHERE ModuleName = N'PatientApp' AND ISNULL(DeleteStatus, 0) = 0
);

IF @DoctorRoleId IS NULL OR @ClinicModuleId IS NULL
BEGIN
    RAISERROR('Doctor role or Clinic module missing.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#ApptReqMenus') IS NOT NULL DROP TABLE #ApptReqMenus;
SELECT * INTO #ApptReqMenus FROM (VALUES
    (N'Doctor',  N'Clinic',     N'Appointment requests', N'/doctor/appointment-requests',  161, N'ri-mail-check-line'),
    (N'Patient', N'PatientApp', N'Appointment requests', N'/patient/appointment-requests', 145, N'ri-mail-send-line')
) v(RoleName, ModuleName, MenuName, MenuUrl, SeqNo, Icon);

BEGIN TRAN;

DECLARE @RoleName NVARCHAR(50), @ModuleName NVARCHAR(100), @MenuName NVARCHAR(200),
        @MenuUrl NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100);
DECLARE @RoleId INT, @ModuleId INT, @MenuId INT;

DECLARE menu_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName, ModuleName, MenuName, MenuUrl, SeqNo, Icon FROM #ApptReqMenus ORDER BY SeqNo;
OPEN menu_cursor;
FETCH NEXT FROM menu_cursor INTO @RoleName, @ModuleName, @MenuName, @MenuUrl, @SeqNo, @Icon;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @RoleId = CASE @RoleName
        WHEN N'Doctor' THEN @DoctorRoleId
        WHEN N'Patient' THEN @PatientRoleId
        ELSE NULL END;
    SET @ModuleId = CASE @ModuleName
        WHEN N'Clinic' THEN @ClinicModuleId
        WHEN N'PatientApp' THEN @PatientModuleId
        ELSE NULL END;

    IF @RoleId IS NULL OR @ModuleId IS NULL
    BEGIN
        PRINT CONCAT('SKIP missing role/module for ', @MenuUrl);
    END
    ELSE
    BEGIN
        SET @MenuId = (
            SELECT TOP 1 MenuId FROM dbo.MenuMaster
            WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0
            ORDER BY MenuId
        );

        IF @MenuId IS NULL
        BEGIN
            INSERT INTO dbo.MenuMaster
                (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
                 MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
                 EnteredBy, EnteredDate, DeleteStatus)
            VALUES
                (@ModuleId, @MenuName, N'', N'Menu', NULL, @MenuUrl, N'Appointment ask-time inbox',
                 @Icon, NULL, NULL, 1, 1, @SeqNo, N'0', N'PARITY-010', GETUTCDATE(), 0);
            SET @MenuId = SCOPE_IDENTITY();
            PRINT CONCAT('INSERTED MenuMaster ', @MenuUrl, ' MenuId=', @MenuId);
        END
        ELSE
        BEGIN
            UPDATE dbo.MenuMaster
            SET MenuName = @MenuName,
                MenuIcon = @Icon,
                ShowInMainMenu = 1,
                IsLeaf = 1,
                DeleteStatus = 0,
                SeqNo = ISNULL(SeqNo, @SeqNo)
            WHERE MenuId = @MenuId;
            PRINT CONCAT('UPDATED MenuMaster ', @MenuUrl, ' MenuId=', @MenuId);
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @RoleId AND MenuId = @MenuId)
        BEGIN
            INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
            VALUES (@RoleId, @MenuId, 1, 1, 1, 1);
            PRINT CONCAT('INSERTED RoleDetails RoleId=', @RoleId, ' MenuId=', @MenuId);
        END
        ELSE
        BEGIN
            UPDATE dbo.RoleDetails
            SET IsView = 1
            WHERE RoleId = @RoleId AND MenuId = @MenuId AND ISNULL(IsView, 0) = 0;
            PRINT CONCAT('SKIP RoleDetails already mapped RoleId=', @RoleId, ' MenuId=', @MenuId);
        END
    END

    FETCH NEXT FROM menu_cursor INTO @RoleName, @ModuleName, @MenuName, @MenuUrl, @SeqNo, @Icon;
END

CLOSE menu_cursor;
DEALLOCATE menu_cursor;
DROP TABLE #ApptReqMenus;

COMMIT TRAN;

PRINT '010_Seed_Appointment_Requests_Menus.sql completed.';
SELECT r.RoleName, m.MenuName, m.MenuUrl, m.MenuIcon, m.ShowInMainMenu
FROM dbo.RoleDetails rd
INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
INNER JOIN dbo.MenuMaster m ON m.MenuId = rd.MenuId
WHERE m.MenuUrl IN (N'/doctor/appointment-requests', N'/patient/appointment-requests')
  AND ISNULL(rd.IsView, 0) = 1
ORDER BY r.RoleName;
GO

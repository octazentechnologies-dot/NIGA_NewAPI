/*
S4 Week 4 — FIN-11 / Clinic menus for Account finance extras, doctor consult fees,
admin trust queue, patient continuity (idempotent).
*/
SET NOCOUNT ON;
GO

DECLARE @AccountRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Account' AND ISNULL(DeleteStatus,0)=0);
DECLARE @DoctorRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Doctor' AND ISNULL(DeleteStatus,0)=0);
DECLARE @AdminRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Admin' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PatientRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Patient' AND ISNULL(DeleteStatus,0)=0);

DECLARE @AccountModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Account' AND ISNULL(DeleteStatus,0)=0);
DECLARE @ClinicModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus,0)=0);
DECLARE @AdminModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'AdminPortal' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PatientModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'PatientApp' AND ISNULL(DeleteStatus,0)=0);

IF OBJECT_ID('tempdb..#S4Menus') IS NOT NULL DROP TABLE #S4Menus;
SELECT * INTO #S4Menus FROM (VALUES
    (N'Account', N'Account', N'Consultation recon', N'/account/consult-recon', 112, N'ri-exchange-line'),
    (N'Account', N'Account', N'Medicine ledger', N'/account/medicine-ledger', 115, N'ri-capsule-line'),
    (N'Account', N'Account', N'Refunds', N'/account/refunds', 135, N'ri-refund-2-line'),
    (N'Account', N'Account', N'Settlements', N'/account/settlements', 136, N'ri-shake-hands-line'),
    (N'Account', N'Account', N'Exceptions', N'/account/exceptions', 137, N'ri-error-warning-line'),
    (N'Account', N'Account', N'GST & tax', N'/account/tax', 138, N'ri-percent-line'),
    (N'Account', N'Account', N'Payees', N'/account/payees', 139, N'ri-bank-line'),
    (N'Account', N'Account', N'Clinic collections', N'/account/clinic-collections', 145, N'ri-store-2-line'),
    (N'Doctor', N'Clinic', N'Consult fees', N'/doctor/consult-fees', 125, N'ri-money-rupee-circle-line'),
    (N'Doctor', N'Clinic', N'Earnings', N'/doctor/earnings', 126, N'ri-line-chart-line'),
    (N'Doctor', N'Clinic', N'Sign eRx', N'/doctor/erx', 128, N'ri-file-list-3-line'),
    (N'Admin', N'AdminPortal', N'Trust queue', N'/admin/trust-queue', 55, N'ri-shield-check-line'),
    (N'Admin', N'AdminPortal', N'HomeoMeds exceptions', N'/admin/homemeds-exceptions', 56, N'ri-capsule-line'),
    (N'Admin', N'AdminPortal', N'Pharmacy partners', N'/admin/pharmacy-partners', 57, N'ri-store-3-line'),
    (N'Admin', N'AdminPortal', N'Consult payments', N'/admin/consult-payments', 58, N'ri-money-rupee-circle-line'),
    (N'Patient', N'PatientApp', N'Care continuity', N'/patient/continuity', 120, N'ri-heart-pulse-line'),
    (N'Patient', N'PatientApp', N'Medicine orders', N'/patient/medicine-orders', 130, N'ri-capsule-line')
) v(RoleName, ModuleName, MenuName, MenuUrl, SeqNo, Icon);

DECLARE @RoleName NVARCHAR(50), @ModuleName NVARCHAR(100), @MenuName NVARCHAR(200),
        @MenuUrl NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100);
DECLARE @RoleId INT, @ModuleId INT, @MenuId INT;

DECLARE s4_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName, ModuleName, MenuName, MenuUrl, SeqNo, Icon FROM #S4Menus ORDER BY SeqNo;
OPEN s4_cursor;
FETCH NEXT FROM s4_cursor INTO @RoleName, @ModuleName, @MenuName, @MenuUrl, @SeqNo, @Icon;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @RoleId = CASE @RoleName
        WHEN N'Account' THEN @AccountRoleId
        WHEN N'Doctor' THEN @DoctorRoleId
        WHEN N'Admin' THEN @AdminRoleId
        WHEN N'Patient' THEN @PatientRoleId
        ELSE NULL END;
    SET @ModuleId = CASE @ModuleName
        WHEN N'Account' THEN @AccountModuleId
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
            (@ModuleId, @MenuName, N'', N'Menu', NULL, @MenuUrl, N'S4 Week 4 menu',
             @Icon, NULL, NULL, 1, 1, @SeqNo, N'0', N'S4-W4', GETUTCDATE(), 0);
        SET @MenuId = SCOPE_IDENTITY();
    END
    ELSE IF @MenuId IS NOT NULL
    BEGIN
        UPDATE dbo.MenuMaster
        SET SeqNo = @SeqNo,
            MenuIcon = CASE WHEN ISNULL(MenuIcon, N'') = N'' THEN @Icon ELSE MenuIcon END,
            ChangedBy = N'S4-W4',
            ChangedDate = GETUTCDATE()
        WHERE MenuId = @MenuId;
    END

    IF @MenuId IS NOT NULL AND @RoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @RoleId AND MenuId = @MenuId)
        INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
        VALUES (@RoleId, @MenuId, 1, 0, 0, 0);
    ELSE IF @MenuId IS NOT NULL AND @RoleId IS NOT NULL
        UPDATE dbo.RoleDetails SET IsView = 1 WHERE RoleId = @RoleId AND MenuId = @MenuId AND IsView = 0;

    -- Alias: older Doctor Earnings URL /account/earnings → keep RoleDetails on /account/doctor-earnings too
    IF @MenuUrl = N'/account/doctor-earnings' AND @AccountRoleId IS NOT NULL
    BEGIN
        DECLARE @EarnAlias INT = (
            SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl = N'/account/earnings' AND ISNULL(DeleteStatus,0)=0
        );
        IF @EarnAlias IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @AccountRoleId AND MenuId = @EarnAlias)
            INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
            VALUES (@AccountRoleId, @EarnAlias, 1, 0, 0, 0);
    END

    FETCH NEXT FROM s4_cursor INTO @RoleName, @ModuleName, @MenuName, @MenuUrl, @SeqNo, @Icon;
END
CLOSE s4_cursor;
DEALLOCATE s4_cursor;
DROP TABLE #S4Menus;

-- Align Accountdashboard home link used by SPA
DECLARE @AcctHome INT = (SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl IN (N'/accountdashboard', N'/account/home') AND ISNULL(DeleteStatus,0)=0 ORDER BY CASE WHEN MenuUrl = N'/accountdashboard' THEN 0 ELSE 1 END);
IF @AcctHome IS NOT NULL AND @AccountRoleId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @AccountRoleId AND MenuId = @AcctHome)
    INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
    VALUES (@AccountRoleId, @AcctHome, 1, 0, 0, 0);

PRINT '02_S4_Week4_Menus.sql completed.';
GO

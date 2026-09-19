/*
================================================================================
Author       : Tufan Powar
Created      : 17-09-2026
Script       : 04_S1_Week1_Mobile_Menus_And_Prefs.sql
Purpose      : Patient Family/Caregiver menus plus UserAppPreference, WelcomeSlide, DevicePushToken.
Use          : Run after scripts 01 and 02.
Prerequisites: RoleMaster Patient; ModuleMaster, MenuMaster, RoleDetails.
Idempotent   : Yes. Creates missing tables/columns; inserts menus/slides only when absent.
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

DECLARE @PatientRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'Patient' AND ISNULL(DeleteStatus, 0) = 0
);

IF @PatientRoleId IS NULL
BEGIN
    RAISERROR('RoleMaster missing Patient. Run 01_SEC_M01_Foundation_Security_Server.sql first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.ModuleMaster WHERE ModuleName = N'PatientApp' AND ISNULL(DeleteStatus, 0) = 0)
BEGIN
    INSERT INTO dbo.ModuleMaster
        (ModuleName, ModuleMarathiName, ModuleIcon, ModuleAreaName, Seqno, IsDirectNode,
         ActionName, ControllerName, ModuleUrl, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (N'PatientApp', N'', N'ri-group-line', N'Patient', 920, 1,
         NULL, NULL, N'/family', N'S1', GETUTCDATE(), 0);
END

DECLARE @PatientModuleId INT = (
    SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'PatientApp' AND ISNULL(DeleteStatus, 0) = 0
);

IF @PatientModuleId IS NULL
BEGIN
    RAISERROR('PatientApp ModuleMaster row could not be resolved.', 16, 1);
    RETURN;
END

IF OBJECT_ID('tempdb..#PatientMenus') IS NOT NULL DROP TABLE #PatientMenus;
SELECT * INTO #PatientMenus FROM (VALUES
    (N'Family',    N'/family',    100, N'ri-group-line'),
    (N'Caregiver', N'/caregiver', 110, N'ri-user-heart-line')
) v(MenuName, MenuUrl, SeqNo, Icon);

DECLARE @MenuName NVARCHAR(200), @MenuUrl NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100), @MenuId INT;
DECLARE patient_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MenuName, MenuUrl, SeqNo, Icon FROM #PatientMenus;
OPEN patient_cursor;
FETCH NEXT FROM patient_cursor INTO @MenuName, @MenuUrl, @SeqNo, @Icon;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0)
    BEGIN
        INSERT INTO dbo.MenuMaster
            (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
             MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
             EnteredBy, EnteredDate, DeleteStatus)
        VALUES
            (@PatientModuleId, @MenuName, N'', N'Menu', NULL, @MenuUrl, N'Patient app (M16/M17)',
             @Icon, NULL, NULL, 1, 1, @SeqNo, N'0', N'S1', GETUTCDATE(), 0);
    END

    SELECT @MenuId = MenuId FROM dbo.MenuMaster WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0;
    IF @MenuId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @PatientRoleId AND MenuId = @MenuId)
    BEGIN
        INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
        VALUES (@PatientRoleId, @MenuId, 1, 0, 0, 0);
    END

    FETCH NEXT FROM patient_cursor INTO @MenuName, @MenuUrl, @SeqNo, @Icon;
END
CLOSE patient_cursor;
DEALLOCATE patient_cursor;
DROP TABLE #PatientMenus;
GO

IF OBJECT_ID(N'dbo.UserAppPreference', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAppPreference
    (
        UserId BIGINT NOT NULL,
        PreferredLanguageId INT NULL,
        WelcomeVersionSeen NVARCHAR(20) NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_UserAppPreference_UpdatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_UserAppPreference PRIMARY KEY CLUSTERED (UserId)
    );
END
GO

IF OBJECT_ID(N'dbo.UserAppPreference', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.UserAppPreference', N'PreferredLanguageId') IS NULL
    ALTER TABLE dbo.UserAppPreference ADD PreferredLanguageId INT NULL;
IF OBJECT_ID(N'dbo.UserAppPreference', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.UserAppPreference', N'WelcomeVersionSeen') IS NULL
    ALTER TABLE dbo.UserAppPreference ADD WelcomeVersionSeen NVARCHAR(20) NULL;
GO

IF OBJECT_ID(N'dbo.WelcomeSlide', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WelcomeSlide
    (
        WelcomeSlideId BIGINT IDENTITY(1,1) NOT NULL,
        Audience NVARCHAR(30) NOT NULL,
        SortOrder INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        Body NVARCHAR(2000) NOT NULL,
        Version NVARCHAR(20) NOT NULL CONSTRAINT DF_WelcomeSlide_Version DEFAULT (N'1'),
        IsActive BIT NOT NULL CONSTRAINT DF_WelcomeSlide_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_WelcomeSlide_CreatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_WelcomeSlide PRIMARY KEY CLUSTERED (WelcomeSlideId)
    );
END
GO

IF OBJECT_ID(N'dbo.WelcomeSlide', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.WelcomeSlide', N'IsActive') IS NULL
    ALTER TABLE dbo.WelcomeSlide ADD IsActive BIT NOT NULL CONSTRAINT DF_WelcomeSlide_IsActive DEFAULT (1);
GO

IF OBJECT_ID(N'dbo.WelcomeSlide', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.WelcomeSlide (Audience, SortOrder, Title, Body, Version, IsActive)
    SELECT v.Audience, v.SortOrder, v.Title, v.Body, v.Version, v.IsActive
    FROM (VALUES
        (N'Patient', 1, N'Welcome to Homeocentrum', N'Your homeopathy care in one place — appointments, family, and prescriptions.', N'1', 1),
        (N'Patient', 2, N'Family first', N'Add family members and book for them with one account.', N'1', 1),
        (N'Patient', 3, N'Your privacy', N'We ask for consent before sharing or recording. You can withdraw anytime.', N'1', 1)
    ) v(Audience, SortOrder, Title, Body, Version, IsActive)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.WelcomeSlide s
        WHERE s.Audience = v.Audience AND s.Title = v.Title
    );
END
GO

IF OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DevicePushToken
    (
        DevicePushTokenId BIGINT IDENTITY(1,1) NOT NULL,
        UserId BIGINT NOT NULL,
        Platform NVARCHAR(20) NOT NULL,
        Token NVARCHAR(512) NOT NULL,
        DeviceId NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_DevicePushToken_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_DevicePushToken_UpdatedAt DEFAULT (GETUTCDATE()),
        DeleteStatus BIT NOT NULL CONSTRAINT DF_DevicePushToken_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_DevicePushToken PRIMARY KEY CLUSTERED (DevicePushTokenId)
    );
END
GO

IF OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DevicePushToken', N'DeviceId') IS NULL
    ALTER TABLE dbo.DevicePushToken ADD DeviceId NVARCHAR(100) NULL;
IF OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DevicePushToken', N'DeleteStatus') IS NULL
    ALTER TABLE dbo.DevicePushToken ADD DeleteStatus BIT NOT NULL CONSTRAINT DF_DevicePushToken_DeleteStatus DEFAULT (0);
IF OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DevicePushToken_UserId_Token' AND object_id = OBJECT_ID(N'dbo.DevicePushToken'))
    CREATE INDEX IX_DevicePushToken_UserId_Token ON dbo.DevicePushToken (UserId, Token);
GO

PRINT '04_S1_Week1_Mobile_Menus_And_Prefs.sql completed.';
GO

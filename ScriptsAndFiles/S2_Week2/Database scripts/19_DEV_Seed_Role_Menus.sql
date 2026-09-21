/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 19_DEV_Seed_Role_Menus.sql
Purpose      : Grant MenuMaster + RoleDetails.IsView for every live web role so
               GET /api/mastersAPI/GetMenuByRole is dynamic (ADM-B04.02/.03).
               Admin = clinical SPA tree. Reception = dashboard only (no case
               taking). Doctor loses Enquiries. Pharmacy gets remaining stubs.
Use          : HomeoCentrum_Dev after S1 02/04/09 and S2 13/15.
Idempotent   : Yes. Inserts missing menus/role-details; revokes Doctor Enquiries.
Do not run   : Production (menu grants only — safe-ish but Dev-tested).
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

DECLARE @AdminRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Admin' AND ISNULL(DeleteStatus,0)=0);
DECLARE @DoctorRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Doctor' AND ISNULL(DeleteStatus,0)=0);
DECLARE @ReceptionRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Reception' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PatientRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Patient' AND ISNULL(DeleteStatus,0)=0);
DECLARE @AccountRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'Account' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PharmacyRoleId INT = (SELECT TOP 1 RoleId FROM dbo.RoleMaster WHERE RoleName = N'PharmacyPartner' AND ISNULL(DeleteStatus,0)=0);

IF @AdminRoleId IS NULL OR @DoctorRoleId IS NULL OR @ReceptionRoleId IS NULL
   OR @PatientRoleId IS NULL OR @AccountRoleId IS NULL OR @PharmacyRoleId IS NULL
BEGIN
    RAISERROR('RoleMaster missing Admin/Doctor/Reception/Patient/Account/PharmacyPartner.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.ModuleMaster WHERE ModuleName = N'AdminPortal' AND ISNULL(DeleteStatus,0)=0)
BEGIN
    INSERT INTO dbo.ModuleMaster
        (ModuleName, ModuleMarathiName, ModuleIcon, ModuleAreaName, Seqno, IsDirectNode,
         ActionName, ControllerName, ModuleUrl, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (N'AdminPortal', N'', N'ri-shield-user-line', N'Admin', 700, 1,
         NULL, NULL, N'/dashboard', N'S2-19', GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.ModuleMaster WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus,0)=0)
BEGIN
    INSERT INTO dbo.ModuleMaster
        (ModuleName, ModuleMarathiName, ModuleIcon, ModuleAreaName, Seqno, IsDirectNode,
         ActionName, ControllerName, ModuleUrl, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (N'Clinic', N'', N'ri-stethoscope-line', N'Clinic', 800, 1,
         NULL, NULL, N'/doctordashboard', N'S2-19', GETUTCDATE(), 0);
END

IF NOT EXISTS (SELECT 1 FROM dbo.ModuleMaster WHERE ModuleName = N'Pharmacy' AND ISNULL(DeleteStatus,0)=0)
BEGIN
    INSERT INTO dbo.ModuleMaster
        (ModuleName, ModuleMarathiName, ModuleIcon, ModuleAreaName, Seqno, IsDirectNode,
         ActionName, ControllerName, ModuleUrl, EnteredBy, EnteredDate, DeleteStatus)
    VALUES
        (N'Pharmacy', N'', N'ri-capsule-line', N'Pharmacy', 910, 1,
         NULL, NULL, N'/pharmacy/home', N'S2-19', GETUTCDATE(), 0);
END

DECLARE @AdminModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'AdminPortal' AND ISNULL(DeleteStatus,0)=0);
DECLARE @ClinicModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Clinic' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PharmacyModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Pharmacy' AND ISNULL(DeleteStatus,0)=0);
DECLARE @AccountModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'Account' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PatientModuleId INT = (SELECT TOP 1 ModuleId FROM dbo.ModuleMaster WHERE ModuleName = N'PatientApp' AND ISNULL(DeleteStatus,0)=0);

IF OBJECT_ID('tempdb..#RoleMenus') IS NOT NULL DROP TABLE #RoleMenus;
CREATE TABLE #RoleMenus (
    RoleName NVARCHAR(50) NOT NULL,
    ModuleName NVARCHAR(100) NOT NULL,
    MenuName NVARCHAR(200) NOT NULL,
    MenuUrl NVARCHAR(200) NOT NULL,
    ParentName NVARCHAR(200) NULL,
    SeqNo INT NOT NULL,
    Icon NVARCHAR(100) NOT NULL,
    IsLeaf BIT NOT NULL
);

-- Admin portal tree (LayoutMenuData Admin Side). Parent rows use unique /admin/nav/* URLs.
INSERT INTO #RoleMenus (RoleName, ModuleName, MenuName, MenuUrl, ParentName, SeqNo, Icon, IsLeaf) VALUES
    (N'Admin', N'AdminPortal', N'Dashboard', N'/dashboard', NULL, 10, N'ri-dashboard-2-line', 1),
    (N'Admin', N'AdminPortal', N'Enquiries', N'/admin/enquiries', NULL, 20, N'ri-mail-line', 1),
    (N'Admin', N'AdminPortal', N'Existance Questions', N'/admin/nav/existancequestions', NULL, 100, N'ri-question-line', 0),
    (N'Admin', N'AdminPortal', N'Existance', N'/admin/listexistance', N'Existance Questions', 101, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Question Group', N'/admin/listquestiongroup', N'Existance Questions', 102, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Sub Question Group', N'/admin/listsubquestiongroup', N'Existance Questions', 103, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Clinical Questions', N'/admin/listclinicalquestion', N'Existance Questions', 104, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Clinical Patterns', N'/admin/nav/clinicalpatterns', NULL, 200, N'ri-bar-chart-horizontal-fill', 0),
    (N'Admin', N'AdminPortal', N'Diagnosis System', N'/admin/listdiagnosissystem', N'Clinical Patterns', 201, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Diagnosis Therapeutics', N'/admin/listdiagnosistherapeuticsdetails', N'Clinical Patterns', 202, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Diagnosis & Conditions', N'/admin/listdiagnosisconditions', N'Clinical Patterns', 203, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Repertory', N'/admin/nav/repertory', NULL, 300, N'ri-book-3-line', 0),
    (N'Admin', N'AdminPortal', N'Section', N'/admin/listsection', N'Repertory', 301, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Sub Section', N'/admin/listsubsection', N'Repertory', 302, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Add Remedies', N'/admin/listrubrics', N'Repertory', 303, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Remedial Rubrics', N'/admin/listremedialrubrics', N'Repertory', 304, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Language', N'/admin/listlanguage', N'Repertory', 305, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Body Parts', N'/admin/listbodyparts', N'Repertory', 306, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Intensity', N'/admin/listintensity', N'Repertory', 307, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Remedy', N'/admin/listremedy', N'Repertory', 308, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Remedy Grade', N'/admin/listremedygrade', N'Repertory', 309, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Materia Medica', N'/admin/nav/materiamedica', NULL, 400, N'ri-hail-line', 0),
    (N'Admin', N'AdminPortal', N'Author', N'/admin/listauthor', N'Materia Medica', 401, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Materia Medica Remedies', N'/admin/listmateriamedicaremedies', N'Materia Medica', 402, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Materia Medica List', N'/admin/listmateriamedica', N'Materia Medica', 403, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Head', N'/admin/listhead', N'Materia Medica', 404, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Adverse Effect', N'/admin/nav/adverseeffect', NULL, 500, N'ri-body-scan-fill', 0),
    (N'Admin', N'AdminPortal', N'Drug System', N'/admin/listdrugsystem', N'Adverse Effect', 501, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Drug Group', N'/admin/listdruggroup', N'Adverse Effect', 502, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Allopathic Drug', N'/admin/listallopathicdrug', N'Adverse Effect', 503, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Deep Analytics', N'/admin/nav/deepanalytics', NULL, 600, N'ri-user-search-line', 0),
    (N'Admin', N'AdminPortal', N'Deep Analytics Home', N'/admin/commingsoon', N'Deep Analytics', 601, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'BU Mgmt.', N'/admin/nav/businessmanagement', NULL, 700, N'ri-customer-service-2-line', 0),
    (N'Admin', N'AdminPortal', N'Packages', N'/admin/listpackage', N'BU Mgmt.', 701, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Qualifications', N'/admin/listqualification', N'BU Mgmt.', 702, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Roles', N'/admin/listrole', N'BU Mgmt.', 703, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Users', N'/admin/listusers', N'BU Mgmt.', 704, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Blogs', N'/admin/listblog', N'BU Mgmt.', 705, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'News', N'/admin/listnews', N'BU Mgmt.', 706, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Labs & Imaging', N'/admin/listlabsimaging', N'BU Mgmt.', 707, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'3D Parts', N'/admin/nav/3dbodypart', NULL, 800, N'ri-body-scan-line', 0),
    (N'Admin', N'AdminPortal', N'Mesh Key Master', N'/admin/listmeshkeymaster', N'3D Parts', 801, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Section Master', N'/admin/list3dsectionmaster', N'3D Parts', 802, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Hotspots', N'/admin/list3dhotspots', N'3D Parts', 803, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Rubric Intelligence', N'/admin/nav/rubricintelligence', NULL, 900, N'ri-brain-line', 0),
    (N'Admin', N'AdminPortal', N'Metaphors', N'/admin/listrubricmetaphors', N'Rubric Intelligence', 901, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Aliases', N'/admin/listrubricaliases', N'Rubric Intelligence', 902, N'ri-list-check', 1),
    (N'Admin', N'AdminPortal', N'Benchmark', N'/admin/rubric-intelligence-benchmark', N'Rubric Intelligence', 903, N'ri-list-check', 1),

    (N'Doctor', N'Clinic', N'Dashboard', N'/doctordashboard', NULL, 100, N'ri-dashboard-2-line', 1),
    (N'Doctor', N'Clinic', N'Patient Board', N'/doctor/patientboard', NULL, 110, N'ri-user-heart-line', 1),
    (N'Doctor', N'Clinic', N'Anatomy', N'/doctor/anatomy', NULL, 120, N'ri-body-scan-line', 1),
    (N'Doctor', N'Clinic', N'Reception Staff', N'/doctor/reception-staff', NULL, 140, N'ri-user-star-line', 1),
    (N'Doctor', N'Clinic', N'Profile', N'/profile', NULL, 150, N'ri-user-settings-line', 1),

    (N'Reception', N'Clinic', N'Dashboard', N'/doctordashboard', NULL, 100, N'ri-dashboard-2-line', 1),

    (N'Patient', N'PatientApp', N'Family', N'/family', NULL, 100, N'ri-group-line', 1),
    (N'Patient', N'PatientApp', N'Caregiver', N'/caregiver', NULL, 110, N'ri-user-heart-line', 1),

    (N'Account', N'Account', N'Account Home', N'/account/home', NULL, 100, N'ri-home-line', 1),
    (N'Account', N'Account', N'Ledger', N'/account/ledger', NULL, 110, N'ri-book-line', 1),
    (N'Account', N'Account', N'Doctor Earnings', N'/account/earnings', NULL, 120, N'ri-money-dollar-circle-line', 1),
    (N'Account', N'Account', N'Payouts', N'/account/payouts', NULL, 130, N'ri-bank-card-line', 1),
    (N'Account', N'Account', N'Invoices', N'/account/invoices', NULL, 140, N'ri-file-list-3-line', 1),
    (N'Account', N'Account', N'Reports', N'/account/reports', NULL, 150, N'ri-bar-chart-line', 1),

    (N'PharmacyPartner', N'Pharmacy', N'Pharmacy Home', N'/pharmacy/home', NULL, 200, N'ri-capsule-line', 1),
    (N'PharmacyPartner', N'Pharmacy', N'Onboarding', N'/pharmacy/onboarding', NULL, 210, N'ri-user-add-line', 1),
    (N'PharmacyPartner', N'Pharmacy', N'Orders', N'/pharmacy/orders', NULL, 220, N'ri-shopping-bag-3-line', 1),
    (N'PharmacyPartner', N'Pharmacy', N'Quotes', N'/pharmacy/quotes', NULL, 230, N'ri-file-list-3-line', 1);

DECLARE @RoleName NVARCHAR(50), @ModuleName NVARCHAR(100), @MenuName NVARCHAR(200),
        @MenuUrl NVARCHAR(200), @ParentName NVARCHAR(200), @SeqNo INT, @Icon NVARCHAR(100), @IsLeaf BIT;
DECLARE @RoleId INT, @ModuleId INT, @MenuId INT, @ParentMenuId INT;

DECLARE menu_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName, ModuleName, MenuName, MenuUrl, ParentName, SeqNo, Icon, IsLeaf
    FROM #RoleMenus
    ORDER BY CASE WHEN ParentName IS NULL THEN 0 ELSE 1 END, SeqNo;
OPEN menu_cursor;
FETCH NEXT FROM menu_cursor INTO @RoleName, @ModuleName, @MenuName, @MenuUrl, @ParentName, @SeqNo, @Icon, @IsLeaf;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @RoleId = CASE @RoleName
        WHEN N'Admin' THEN @AdminRoleId
        WHEN N'Doctor' THEN @DoctorRoleId
        WHEN N'Reception' THEN @ReceptionRoleId
        WHEN N'Patient' THEN @PatientRoleId
        WHEN N'Account' THEN @AccountRoleId
        WHEN N'PharmacyPartner' THEN @PharmacyRoleId
        ELSE NULL END;
    SET @ModuleId = CASE @ModuleName
        WHEN N'AdminPortal' THEN @AdminModuleId
        WHEN N'Clinic' THEN @ClinicModuleId
        WHEN N'Pharmacy' THEN @PharmacyModuleId
        WHEN N'Account' THEN @AccountModuleId
        WHEN N'PatientApp' THEN @PatientModuleId
        ELSE NULL END;

    SET @MenuId = (
        SELECT TOP 1 MenuId FROM dbo.MenuMaster
        WHERE MenuUrl = @MenuUrl AND ISNULL(DeleteStatus, 0) = 0
        ORDER BY MenuId
    );
    IF @MenuId IS NULL
        SET @MenuId = (
            SELECT TOP 1 MenuId FROM dbo.MenuMaster
            WHERE MenuName = @MenuName AND ModuleId = @ModuleId AND ISNULL(DeleteStatus, 0) = 0
            ORDER BY MenuId
        );

    SET @ParentMenuId = NULL;
    IF @ParentName IS NOT NULL
        SET @ParentMenuId = (
            SELECT TOP 1 MenuId FROM dbo.MenuMaster
            WHERE MenuName = @ParentName AND ModuleId = @ModuleId AND ISNULL(DeleteStatus, 0) = 0
            ORDER BY MenuId
        );

    IF @MenuId IS NULL AND @ModuleId IS NOT NULL
    BEGIN
        INSERT INTO dbo.MenuMaster
            (ModuleId, MenuName, MenuNameMarathi, MenuType, ParentMenuId, MenuUrl, Description,
             MenuIcon, ActionName, ControllerName, IsLeaf, ShowInMainMenu, SeqNo, FirmIds,
             EnteredBy, EnteredDate, DeleteStatus)
        VALUES
            (@ModuleId, @MenuName, N'', N'Menu', @ParentMenuId, @MenuUrl, N'Role menu seed S2-19',
             @Icon, NULL, NULL, @IsLeaf, 1, @SeqNo, N'0', N'S2-19', GETUTCDATE(), 0);
        SET @MenuId = SCOPE_IDENTITY();
    END
    ELSE IF @MenuId IS NOT NULL
    BEGIN
        UPDATE dbo.MenuMaster
        SET ParentMenuId = COALESCE(@ParentMenuId, ParentMenuId),
            SeqNo = @SeqNo,
            MenuIcon = CASE WHEN ISNULL(MenuIcon, N'') = N'' THEN @Icon ELSE MenuIcon END,
            IsLeaf = @IsLeaf,
            ChangedBy = N'S2-19',
            ChangedDate = GETUTCDATE()
        WHERE MenuId = @MenuId;
    END

    IF @MenuId IS NOT NULL AND @RoleId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @RoleId AND MenuId = @MenuId)
    BEGIN
        INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
        VALUES (@RoleId, @MenuId, 1, 0, 0, 0);
    END
    ELSE IF @MenuId IS NOT NULL AND @RoleId IS NOT NULL
    BEGIN
        UPDATE dbo.RoleDetails SET IsView = 1 WHERE RoleId = @RoleId AND MenuId = @MenuId AND IsView = 0;
    END

    FETCH NEXT FROM menu_cursor INTO @RoleName, @ModuleName, @MenuName, @MenuUrl, @ParentName, @SeqNo, @Icon, @IsLeaf;
END
CLOSE menu_cursor;
DEALLOCATE menu_cursor;

-- Also grant existing /enquiries row to Admin (LayoutMenuData + /enquiries route).
DECLARE @EnquiriesMenuId INT = (
    SELECT TOP 1 MenuId FROM dbo.MenuMaster WHERE MenuUrl = N'/enquiries' AND ISNULL(DeleteStatus,0)=0
);
IF @EnquiriesMenuId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.RoleDetails WHERE RoleId = @AdminRoleId AND MenuId = @EnquiriesMenuId)
    INSERT INTO dbo.RoleDetails (RoleId, MenuId, IsView, IsAdd, IsModify, IsDelete)
    VALUES (@AdminRoleId, @EnquiriesMenuId, 1, 0, 0, 0);

-- CLN-02.02 — Doctor must not have Admin Enquiries in GetMenuByRole.
DELETE rd
FROM dbo.RoleDetails rd
INNER JOIN dbo.MenuMaster m ON m.MenuId = rd.MenuId
WHERE rd.RoleId = @DoctorRoleId
  AND m.MenuUrl IN (N'/enquiries', N'/admin/enquiries');

-- Prevent duplicate RoleId+MenuId grants (RoleDetails PK is RecordId only).
IF COL_LENGTH(N'dbo.RoleDetails', N'RoleId') IS NOT NULL
   AND COL_LENGTH(N'dbo.RoleDetails', N'MenuId') IS NOT NULL
   AND COL_LENGTH(N'dbo.RoleDetails', N'RecordId') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_RoleDetails_RoleId_MenuId'
          AND object_id = OBJECT_ID(N'dbo.RoleDetails')
   )
BEGIN
    ;WITH d AS (
        SELECT RecordId,
               ROW_NUMBER() OVER (PARTITION BY RoleId, MenuId ORDER BY RecordId) AS rn
        FROM dbo.RoleDetails
    )
    DELETE FROM dbo.RoleDetails
    WHERE RecordId IN (SELECT RecordId FROM d WHERE rn > 1);

    CREATE UNIQUE INDEX UX_RoleDetails_RoleId_MenuId
        ON dbo.RoleDetails (RoleId, MenuId);
    PRINT 'CREATED unique index UX_RoleDetails_RoleId_MenuId';
END

DROP TABLE #RoleMenus;

PRINT '19_DEV_Seed_Role_Menus.sql completed.';
PRINT 'Verify GetMenuByRole for Tufan_Admin / Tufan_Doctore / Tufan_Reception / Tufan_Account / Tufan_Pharmacy / Tufan_Patient.';
SELECT r.RoleName, COUNT(*) AS MenuCount
FROM dbo.RoleDetails rd
INNER JOIN dbo.RoleMaster r ON r.RoleId = rd.RoleId
WHERE rd.IsView = 1
GROUP BY r.RoleName
ORDER BY r.RoleName;
GO

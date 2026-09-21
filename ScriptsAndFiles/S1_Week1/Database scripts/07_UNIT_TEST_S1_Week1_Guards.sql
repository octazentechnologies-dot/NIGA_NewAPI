/*
================================================================================
Author       : Tufan Powar
Created      : 17-09-2026
Script       : 07_UNIT_TEST_S1_Week1_Guards.sql
Purpose      : Unit tests for Week 1 SQL guards: missing-table create, missing-column add,
               seed-if-absent, and live HomeoCentrum schema assertions.
Use          : Run after 01–06. Isolated tests use temp tables; live checks read dbo schema.
Idempotent   : Yes. Temp objects are dropped. Live checks are read-only except isolated #tables.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Pass INT = 0;

-- =============================================================================
-- Isolated unit tests (do not touch production tables)
-- =============================================================================

IF OBJECT_ID('tempdb..#UtGuard') IS NOT NULL DROP TABLE #UtGuard;

-- Test 1: table missing -> create
IF OBJECT_ID('tempdb..#UtGuard') IS NULL
BEGIN
    CREATE TABLE #UtGuard
    (
        Id INT NOT NULL PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL
    );
END

IF OBJECT_ID('tempdb..#UtGuard') IS NOT NULL
BEGIN
    SET @Pass += 1;
    PRINT 'PASS: UT-01 create table when missing';
END
ELSE
BEGIN
    SET @Fail += 1;
    PRINT 'FAIL: UT-01 create table when missing';
END

-- Test 2: table exists, column missing -> add
IF NOT EXISTS (
    SELECT 1 FROM tempdb.sys.columns
    WHERE object_id = OBJECT_ID(N'tempdb..#UtGuard') AND name = N'Code'
)
    ALTER TABLE #UtGuard ADD Code NVARCHAR(20) NULL;

IF EXISTS (
    SELECT 1 FROM tempdb.sys.columns
    WHERE object_id = OBJECT_ID(N'tempdb..#UtGuard') AND name = N'Code'
)
BEGIN
    SET @Pass += 1;
    PRINT 'PASS: UT-02 add column when missing';
END
ELSE
BEGIN
    SET @Fail += 1;
    PRINT 'FAIL: UT-02 add column when missing';
END

-- Test 3: seed if absent, skip if present
INSERT INTO #UtGuard (Id, Name, Code)
SELECT 1, N'Patient', N'P'
WHERE NOT EXISTS (SELECT 1 FROM #UtGuard WHERE Id = 1);

INSERT INTO #UtGuard (Id, Name, Code)
SELECT 1, N'Patient', N'P'
WHERE NOT EXISTS (SELECT 1 FROM #UtGuard WHERE Id = 1);

IF (SELECT COUNT(*) FROM #UtGuard WHERE Id = 1) = 1
BEGIN
    SET @Pass += 1;
    PRINT 'PASS: UT-03 insert-if-absent does not duplicate';
END
ELSE
BEGIN
    SET @Fail += 1;
    PRINT 'FAIL: UT-03 insert-if-absent duplicated rows';
END

DROP TABLE #UtGuard;

-- =============================================================================
-- Live schema assertions (HomeoCentrum after scripts 01–05)
-- =============================================================================

IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NULL
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-04 UserMaster exists'; END
ELSE BEGIN SET @Pass += 1; PRINT 'PASS: UT-04 UserMaster exists'; END

IF COL_LENGTH(N'dbo.UserMaster', N'UserPassword') IS NOT NULL
   AND EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = N'UserMaster' AND COLUMN_NAME = N'UserPassword'
          AND CHARACTER_MAXIMUM_LENGTH >= 500
   )
BEGIN SET @Pass += 1; PRINT 'PASS: UT-05 UserPassword NVARCHAR(500+)'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-05 UserPassword NVARCHAR(500+)'; END

IF OBJECT_ID(N'dbo.PasswordResetToken', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PasswordResetToken', N'TokenHash') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: UT-06 PasswordResetToken schema'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-06 PasswordResetToken schema'; END

IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NOT NULL
   AND (SELECT COUNT(*) FROM dbo.ConsentType WHERE Code IN (N'Privacy', N'Booking', N'Caregiver')) >= 3
BEGIN SET @Pass += 1; PRINT 'PASS: UT-07 ConsentType seed rows'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-07 ConsentType seed rows'; END

IF OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.AuditEvent', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.SecureDocument', N'U') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: UT-08 OTP/Audit/SecureDocument exist'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-08 OTP/Audit/SecureDocument exist'; END

IF COL_LENGTH(N'dbo.RoleMaster', N'RoleName') IS NOT NULL
   AND (SELECT COUNT(*) FROM dbo.RoleMaster WHERE RoleName IN (N'Patient', N'Account', N'PharmacyPartner') AND ISNULL(DeleteStatus,0)=0) = 3
BEGIN SET @Pass += 1; PRINT 'PASS: UT-09 RoleMaster Patient/Account/PharmacyPartner'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-09 RoleMaster Patient/Account/PharmacyPartner'; END

IF OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientUserMap', N'IsPrimary') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PatientUserMap_User_Primary' AND object_id = OBJECT_ID(N'dbo.PatientUserMap'))
BEGIN SET @Pass += 1; PRINT 'PASS: UT-10 PatientUserMap + filtered unique index'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-10 PatientUserMap + filtered unique index'; END

IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: UT-11 family/caregiver tables'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-11 family/caregiver tables'; END

IF OBJECT_ID(N'dbo.MenuMaster', N'U') IS NOT NULL
   AND (SELECT COUNT(*) FROM dbo.MenuMaster WHERE MenuUrl IN (N'/account/home', N'/pharmacy/home', N'/family', N'/caregiver') AND ISNULL(DeleteStatus,0)=0) >= 4
BEGIN SET @Pass += 1; PRINT 'PASS: UT-12 Account/Pharmacy/Patient menus'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-12 Account/Pharmacy/Patient menus'; END

IF OBJECT_ID(N'dbo.WelcomeSlide', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.UserAppPreference', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: UT-13 WelcomeSlide/UserAppPreference/DevicePushToken'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-13 WelcomeSlide/UserAppPreference/DevicePushToken'; END

IF COL_LENGTH(N'dbo.UserMaster', N'UserName') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'tufanpowar001@gmail.com' AND ISNULL(DeleteStatus,0)=0)
BEGIN SET @Pass += 1; PRINT 'PASS: UT-14 Patient seed login exists'; END
ELSE
BEGIN SET @Fail += 1; PRINT 'FAIL: UT-14 Patient seed login exists'; END

PRINT '------------------------------------------------';
PRINT CONCAT('S1 Week 1 unit tests: PASS=', @Pass, ' FAIL=', @Fail);

IF @Fail > 0
BEGIN
    RAISERROR('07_UNIT_TEST_S1_Week1_Guards.sql FAILED. See FAIL lines above.', 16, 1);
END
ELSE
    PRINT '07_UNIT_TEST_S1_Week1_Guards.sql completed: ALL PASSED.';
GO

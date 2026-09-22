/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 15_DEV_Seed_Tufan_Role_Logins.sql
Purpose      : Shared team logins for every live web role.
               Username pattern Tufan_<Role>. Password 123456 (same PBKDF2 as
               tufanpowar001@gmail.com). Reception is DoctorReceptionStaff.
Use          : HomeoCentrum_Dev only. Idempotent (update password if login exists).
Do not run   : Production.
Logins       : Tufan_Admin / Tufan_Doctor / Tufan_Reception / Tufan_Account /
               Tufan_Pharmacy / Tufan_Patient / Tufan_Caregiver / Tufan_NoMenu
               Password for all: 123456
               Tufan_NoMenu is an EmptyMenuProbe API user (GetMenuByRole 200 []).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NULL OR OBJECT_ID(N'dbo.RoleMaster', N'U') IS NULL
BEGIN
    RAISERROR('UserMaster / RoleMaster missing.', 16, 1);
    RETURN;
END

DECLARE @Pwd NVARCHAR(500) = (
    SELECT TOP 1 UserPassword
    FROM dbo.UserMaster
    WHERE UserName = N'tufanpowar001@gmail.com'
      AND ISNULL(DeleteStatus, 0) = 0
);
IF @Pwd IS NULL OR LTRIM(RTRIM(@Pwd)) = N''
    SET @Pwd = N'PBKDF2$v1$100000$uszumO1aPL3it0x1tm/FcA==$hvFLmFwfXrC7i3Id07HFiH9ah4WiIAdjzYPyQnArLxQ='; -- 123456

DECLARE @HasFirstName BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'FirstName') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasLastName BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'LastName') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasEmail BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'EmailId') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasMobile BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'MobileNo') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasCountry BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'CountryId') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasState BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'StateId') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasActivated BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'IsUserActivated') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasEnteredBy BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'EnteredBy') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasEnteredDate BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'EnteredDate') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasUserStatus BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'UserStatus') IS NULL THEN 0 ELSE 1 END;

-- BUG-S1-01 probe: a live user whose role has zero RoleDetails (GetMenuByRole must be 200 []).
IF NOT EXISTS (
    SELECT 1 FROM dbo.RoleMaster
    WHERE RoleName = N'EmptyMenuProbe' AND ISNULL(DeleteStatus, 0) = 0
)
BEGIN
    INSERT INTO dbo.RoleMaster (RoleName, FirmIds, DeleteStatus, EnteredBy, EnteredDate)
    VALUES (N'EmptyMenuProbe', N'0', 0, N'TUFAN-TEAM', GETDATE());
    PRINT 'INSERTED RoleMaster EmptyMenuProbe (no menus)';
END

DECLARE @QualId INT = (
    SELECT TOP 1 QualificationID FROM dbo.QualificationMaster
    WHERE ISNULL(DeleteStatus, 0) = 0
    ORDER BY QualificationID
);

DECLARE @Users TABLE
(
    RoleName NVARCHAR(50) NOT NULL,
    UserName NVARCHAR(200) NOT NULL PRIMARY KEY,
    EmailId NVARCHAR(200) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    MobileNo NVARCHAR(20) NOT NULL
);

-- Email/mobile cannot be shared across UserMaster rows: LoginWithOtp and
-- ForgotPassword resolve FirstOrDefault by MobileNo / EmailId.
-- Shared team contact (tufanpowar001@gmail.com / 7768046064) is on Tufan_Patient only.
INSERT INTO @Users (RoleName, UserName, EmailId, FirstName, LastName, MobileNo) VALUES
    (N'Admin',            N'Tufan_Admin',     N'tufan.admin@homeocentrum.dev',     N'Tufan', N'Admin',     N'9000000201'),
    (N'Doctor',           N'Tufan_Doctor',    N'tufan.doctor@homeocentrum.dev',    N'Tufan', N'Doctor',    N'9000000202'),
    (N'Account',          N'Tufan_Account',   N'tufan.account@homeocentrum.dev',   N'Tufan', N'Account',   N'9000000203'),
    (N'PharmacyPartner',  N'Tufan_Pharmacy',  N'tufan.pharmacy@homeocentrum.dev',  N'Tufan', N'Pharmacy',  N'9000000204'),
    (N'Patient',          N'Tufan_Patient',   N'tufanpowar001@gmail.com',             N'Tufan', N'Patient',   N'7768046064'),
    (N'Patient',          N'Tufan_Caregiver', N'tufan.caregiver@homeocentrum.dev', N'Tufan', N'Caregiver', N'9000000206'),
    (N'EmptyMenuProbe',   N'Tufan_NoMenu',    N'tufan.nomenu@homeocentrum.dev',    N'Tufan', N'NoMenu',    N'9000000207');

BEGIN TRAN;

-- Fix typo login Tufan_Doctore → Tufan_Doctor (idempotent).
IF EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Doctore' AND ISNULL(DeleteStatus,0)=0)
   AND NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0)
BEGIN
    UPDATE dbo.UserMaster SET UserName = N'Tufan_Doctor' WHERE UserName = N'Tufan_Doctore' AND ISNULL(DeleteStatus,0)=0;
    IF @HasLastName = 1
        UPDATE dbo.UserMaster SET LastName = N'Doctor' WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0;
    IF @HasEmail = 1
        UPDATE dbo.UserMaster SET EmailId = N'tufan.doctor@homeocentrum.dev' WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0;
    PRINT 'RENAMED UserMaster Tufan_Doctore → Tufan_Doctor';
END

DECLARE @RoleName NVARCHAR(50), @UserName NVARCHAR(200), @Email NVARCHAR(200),
        @First NVARCHAR(100), @Last NVARCHAR(100), @Mobile NVARCHAR(20), @RoleId INT, @UserId BIGINT;
DECLARE @Sql NVARCHAR(MAX), @Vals NVARCHAR(MAX);

DECLARE u_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName, UserName, EmailId, FirstName, LastName, MobileNo FROM @Users;

OPEN u_cursor;
FETCH NEXT FROM u_cursor INTO @RoleName, @UserName, @Email, @First, @Last, @Mobile;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @RoleId = (
        SELECT TOP 1 RoleId FROM dbo.RoleMaster
        WHERE RoleName = @RoleName AND ISNULL(DeleteStatus, 0) = 0
    );

    IF @RoleId IS NULL
        PRINT CONCAT('SKIP role missing: ', @RoleName);
    ELSE
    BEGIN
        -- Match UserName only. Looking up by EmailId would collapse two roles if
        -- they ever shared tufanpowar001@gmail.com (LoginWithOtp/ForgotPassword uniqueness).
        SET @UserId = (
            SELECT TOP 1 UserId FROM dbo.UserMaster
            WHERE ISNULL(DeleteStatus, 0) = 0
              AND UserName = @UserName
        );

        IF @UserId IS NOT NULL
        BEGIN
            UPDATE dbo.UserMaster
            SET UserPassword = @Pwd,
                RoleId = @RoleId,
                EmailId = CASE WHEN @HasEmail = 1 THEN @Email ELSE EmailId END,
                MobileNo = CASE WHEN @HasMobile = 1 THEN @Mobile ELSE MobileNo END,
                UserStatus = CASE WHEN @HasUserStatus = 1 THEN 1 ELSE UserStatus END,
                IsUserActivated = CASE WHEN @HasActivated = 1 THEN 1 ELSE IsUserActivated END,
                DeleteStatus = 0
            WHERE UserId = @UserId;
            PRINT CONCAT('UPDATED UserMaster ', @UserName, ' UserId=', @UserId, ' role=', @RoleName);
        END
        ELSE
        BEGIN
            SET @Sql = N'INSERT INTO dbo.UserMaster (UserName, UserPassword, DeleteStatus, RoleId';
            SET @Vals = N' VALUES (@pUser, @pPwd, 0, @pRole';

            IF @HasUserStatus = 1 BEGIN SET @Sql += N', UserStatus'; SET @Vals += N', 1'; END
            IF @HasMobile = 1 BEGIN SET @Sql += N', MobileNo'; SET @Vals += N', @pMobile'; END
            IF @HasEmail = 1 BEGIN SET @Sql += N', EmailId'; SET @Vals += N', @pEmail'; END
            IF @HasFirstName = 1 BEGIN SET @Sql += N', FirstName'; SET @Vals += N', @pFirst'; END
            IF @HasLastName = 1 BEGIN SET @Sql += N', LastName'; SET @Vals += N', @pLast'; END
            IF @HasCountry = 1 BEGIN SET @Sql += N', CountryId'; SET @Vals += N', 78'; END
            IF @HasState = 1 BEGIN SET @Sql += N', StateId'; SET @Vals += N', 14'; END
            IF @HasActivated = 1 BEGIN SET @Sql += N', IsUserActivated'; SET @Vals += N', 1'; END
            IF @HasEnteredBy = 1 BEGIN SET @Sql += N', EnteredBy'; SET @Vals += N', N''TUFAN-TEAM'''; END
            IF @HasEnteredDate = 1 BEGIN SET @Sql += N', EnteredDate'; SET @Vals += N', GETDATE()'; END

            SET @Sql += N')' + @Vals + N'); SET @pNewId = SCOPE_IDENTITY();';

            EXEC sp_executesql @Sql,
                N'@pUser NVARCHAR(200), @pPwd NVARCHAR(500), @pRole INT, @pMobile NVARCHAR(20), @pEmail NVARCHAR(200), @pFirst NVARCHAR(100), @pLast NVARCHAR(100), @pNewId BIGINT OUTPUT',
                @pUser = @UserName, @pPwd = @Pwd, @pRole = @RoleId,
                @pMobile = @Mobile, @pEmail = @Email, @pFirst = @First, @pLast = @Last,
                @pNewId = @UserId OUTPUT;

            PRINT CONCAT('INSERTED UserMaster ', @UserName, ' UserId=', @UserId, ' role=', @RoleName);
        END
    END

    FETCH NEXT FROM u_cursor INTO @RoleName, @UserName, @Email, @First, @Last, @Mobile;
END

CLOSE u_cursor;
DEALLOCATE u_cursor;

-- Doctor row for Tufan_Doctor (clinic JWT needs Doctor.UserId)
DECLARE @DocUserId BIGINT = (SELECT TOP 1 UserId FROM dbo.UserMaster WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0);
DECLARE @DoctorId INT = NULL;

IF @DocUserId IS NULL
    PRINT 'SKIP Doctor: Tufan_Doctor UserMaster missing';
ELSE IF @DocUserId > 2147483647
    RAISERROR('Tufan_Doctor UserId does not fit Doctor.UserId INT.', 16, 1);
ELSE
BEGIN
    SET @DoctorId = (
        SELECT TOP 1 DoctorID FROM dbo.Doctor
        WHERE UserId = CAST(@DocUserId AS INT) AND ISNULL(DeleteStatus, 0) = 0
    );

    IF @DoctorId IS NULL
    BEGIN
        INSERT INTO dbo.Doctor (
            FirstName, LastName, QualificationID, PermanantAddress, MobileNo, EmailId,
            City, EnteredBy, EnteredDate, DeleteStatus, UserId,
            ClinicName, ConsultFeeInClinic, ConsultFeeTele,
            IsOnline, DirectoryVisible, VerificationStatus, PracticeActivated,
            CountryId, StateId
        )
        VALUES (
            N'Tufan', N'Doctor', @QualId, N'Tufan team clinic',
            N'9000000202', N'tufan.doctor@homeocentrum.dev',
            N'Pune', N'TUFAN-TEAM', GETDATE(), 0, CAST(@DocUserId AS INT),
            N'Tufan Homeopathy Clinic', 550, 420,
            1, 1, N'Verified', 1,
            78, 14
        );
        SET @DoctorId = SCOPE_IDENTITY();
        PRINT CONCAT('INSERTED Doctor DoctorId=', @DoctorId, ' UserId=', @DocUserId);
    END
    ELSE
    BEGIN
        UPDATE dbo.Doctor
        SET LastName = N'Doctor',
            EmailId = N'tufan.doctor@homeocentrum.dev',
            DirectoryVisible = 1,
            VerificationStatus = N'Verified',
            PracticeActivated = 1,
            IsOnline = 1,
            DeleteStatus = 0,
            ConsultFeeInClinic = ISNULL(ConsultFeeInClinic, 550),
            ConsultFeeTele = ISNULL(ConsultFeeTele, 420),
            ClinicName = ISNULL(NULLIF(LTRIM(RTRIM(ClinicName)), N''), N'Tufan Homeopathy Clinic')
        WHERE DoctorID = @DoctorId;
        PRINT CONCAT('UPDATED Doctor DoctorId=', @DoctorId);
    END

    IF OBJECT_ID(N'dbo.DoctorVerification', N'U') IS NOT NULL
       AND @DoctorId IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM dbo.DoctorVerification
            WHERE DoctorId = @DoctorId AND ISNULL(DeleteStatus, 0) = 0
       )
    BEGIN
        INSERT INTO dbo.DoctorVerification (DoctorId, Status, EnteredDate, DeleteStatus)
        VALUES (@DoctorId, N'Approved', GETUTCDATE(), 0);
        PRINT 'INSERTED DoctorVerification Approved for Tufan_Doctor';
    END
END

-- Reception login (not UserMaster)
IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NULL
    PRINT 'SKIP reception: DoctorReceptionStaff missing';
ELSE IF @DoctorId IS NULL
    PRINT 'SKIP reception: Tufan_Doctor DoctorId missing';
ELSE IF EXISTS (
    SELECT 1 FROM dbo.DoctorReceptionStaff
    WHERE UserID = N'Tufan_Reception' AND ISNULL(DeleteStatus, 0) = 0
)
BEGIN
    UPDATE dbo.DoctorReceptionStaff
    SET Password = N'123456',
        DoctorID = @DoctorId,
        ContactNumber = N'7768046064',
        EmailId = N'tufanpowar001@gmail.com',
        DeleteStatus = 0
    WHERE UserID = N'Tufan_Reception';
    PRINT 'UPDATED DoctorReceptionStaff Tufan_Reception';
END
ELSE
BEGIN
    INSERT INTO dbo.DoctorReceptionStaff (
        DoctorID, UserID, Password, FullName, Address, ContactNumber, EmailId,
        Country, State, City, EnteredBy, EnteredDate, DeleteStatus
    )
    VALUES (
        @DoctorId,
        N'Tufan_Reception',
        N'123456',
        N'Tufan Reception',
        N'Tufan team clinic',
        N'7768046064',
        N'tufanpowar001@gmail.com',
        N'India', N'Maharashtra', N'Pune',
        NULL, GETDATE(), 0
    );
    PRINT 'INSERTED DoctorReceptionStaff Tufan_Reception';
END

-- Patient clinical row + map for Tufan_Patient
DECLARE @PatUserId BIGINT = (SELECT TOP 1 UserId FROM dbo.UserMaster WHERE UserName = N'Tufan_Patient' AND ISNULL(DeleteStatus,0)=0);
DECLARE @PatientId INT = NULL;

IF @PatUserId IS NULL
    PRINT 'SKIP Patient: Tufan_Patient UserMaster missing';
ELSE IF OBJECT_ID(N'dbo.Patient', N'U') IS NULL OR OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NULL
    PRINT 'SKIP Patient tables missing';
ELSE
BEGIN
    SET @PatientId = (
        SELECT TOP 1 p.PatientId
        FROM dbo.PatientUserMap m
        INNER JOIN dbo.Patient p ON p.PatientId = m.PatientId
        WHERE m.UserId = @PatUserId AND ISNULL(m.DeleteStatus, 0) = 0 AND ISNULL(m.IsPrimary, 1) = 1
        ORDER BY m.PatientUserMapId DESC
    );

    IF @PatientId IS NULL
    BEGIN
        INSERT INTO dbo.Patient (
            PatientName, Address, StateId, CountryId, MobileNo,
            DateOfBirth, Gender, Email, Age, IsWhatsAppOptIn, DeleteStatus, EnteredBy, EnteredDate
        )
        VALUES (
            N'Tufan Patient',
            N'Tufan team',
            14, 78, N'7768046064',
            '1990-01-01', 0, N'tufanpowar001@gmail.com',
            DATEDIFF(YEAR, '1990-01-01', GETDATE()),
            0,
            0, N'TUFAN-TEAM', GETDATE()
        );
        SET @PatientId = SCOPE_IDENTITY();

        INSERT INTO dbo.PatientUserMap (UserId, PatientId, IsPrimary, DeleteStatus, EnteredBy, EnteredDate)
        VALUES (@PatUserId, @PatientId, 1, 0, N'TUFAN-TEAM', GETUTCDATE());
        PRINT CONCAT('INSERTED Patient+Map PatientId=', @PatientId, ' UserId=', @PatUserId);
    END
    ELSE
    BEGIN
        UPDATE dbo.Patient
        SET MobileNo = N'7768046064',
            Email = N'tufanpowar001@gmail.com'
        WHERE PatientId = @PatientId;
        PRINT CONCAT('UPDATED Patient contact PatientId=', @PatientId);
    END
END

-- Caregiver grant: Tufan_Caregiver acts for Tufan_Patient
DECLARE @CgUserId BIGINT = (SELECT TOP 1 UserId FROM dbo.UserMaster WHERE UserName = N'Tufan_Caregiver' AND ISNULL(DeleteStatus,0)=0);

IF OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NULL
    PRINT 'SKIP caregiver table missing';
ELSE IF @CgUserId IS NULL OR @PatientId IS NULL
    PRINT 'SKIP caregiver grant: user or patient missing';
ELSE IF EXISTS (
    SELECT 1 FROM dbo.CaregiverAuthorization
    WHERE CaregiverUserId = @CgUserId AND PatientId = @PatientId
      AND ISNULL(DeleteStatus, 0) = 0 AND RevokedAt IS NULL
)
    PRINT 'SKIP caregiver grant already active';
ELSE
BEGIN
    INSERT INTO dbo.CaregiverAuthorization (PatientId, CaregiverUserId, GrantedByUserId, GrantedAt, Scope, DeleteStatus)
    VALUES (@PatientId, @CgUserId, @PatUserId, GETUTCDATE(), N'booking', 0);
    PRINT 'INSERTED CaregiverAuthorization Tufan_Caregiver -> Tufan_Patient';
END

COMMIT TRAN;

PRINT '15_DEV_Seed_Tufan_Role_Logins.sql completed.';
PRINT 'All passwords: 123456';
PRINT 'UserMaster: Tufan_Admin Tufan_Doctor Tufan_Account Tufan_Pharmacy Tufan_Patient Tufan_Caregiver Tufan_NoMenu';
PRINT 'Reception (DoctorReceptionStaff): Tufan_Reception';
PRINT 'NEXT: run 19_DEV_Seed_Role_Menus.sql so GetMenuByRole is mapped for every role.';
GO

/*
================================================================================
Script       : 009_PROD_Seed_Gourav_Dev_Role_Logins.sql
Purpose      : Mirror Dev/Stage Gourav_Dev_* role logins on Production for QA.
               Same pattern as Tufan_* / Gourav_Dev_* on Dev (password 123456).
Also         : EmptyMenuProbe role (for NoMenu), UX_RoleDetails_RoleId_MenuId if missing.
Idempotent   : Yes.
Password     : 123456 (PBKDF2 for UserMaster; plaintext for DoctorReceptionStaff
               to match existing Prod Tufan_Reception style).
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

/* Missing unique index from Dev RoleDetails seed */
IF OBJECT_ID(N'dbo.RoleDetails', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.RoleDetails', N'RoleId') IS NOT NULL
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
ELSE
    PRINT 'SKIP index UX_RoleDetails_RoleId_MenuId already present or RoleDetails incomplete';

DECLARE @Pwd NVARCHAR(500) =
    N'PBKDF2$v1$100000$uszumO1aPL3it0x1tm/FcA==$hvFLmFwfXrC7i3Id07HFiH9ah4WiIAdjzYPyQnArLxQ='; -- 123456

DECLARE @Email NVARCHAR(200) = N'gourav.dev@seed.invalid';
DECLARE @Mobile NVARCHAR(20) = N'8001001005';
DECLARE @EnteredBy NVARCHAR(50) = N'GOURAV-SEED';

BEGIN TRAN;

/* EmptyMenuProbe role (Dev RoleId 8) — needed for Gourav_Dev_NoMenu */
IF NOT EXISTS (
    SELECT 1 FROM dbo.RoleMaster
    WHERE RoleName = N'EmptyMenuProbe' AND ISNULL(DeleteStatus, 0) = 0
)
BEGIN
    INSERT INTO dbo.RoleMaster (RoleName, FirmIds, DeleteStatus, EnteredBy, EnteredDate)
    VALUES (N'EmptyMenuProbe', N'0', 0, @EnteredBy, GETDATE());
    PRINT CONCAT('INSERTED RoleMaster EmptyMenuProbe RoleId=', SCOPE_IDENTITY());
END
ELSE
    PRINT 'SKIP RoleMaster EmptyMenuProbe already present';

DECLARE @QualId INT = (
    SELECT TOP 1 QualificationID FROM dbo.QualificationMaster
    WHERE ISNULL(DeleteStatus, 0) = 0
    ORDER BY QualificationID
);

DECLARE @Users TABLE
(
    RoleName NVARCHAR(50) NOT NULL,
    UserName NVARCHAR(200) NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL
);

INSERT INTO @Users (RoleName, UserName, FirstName, LastName) VALUES
    (N'Admin',            N'Gourav_Dev_Admin',     N'Gourav', N'Admin'),
    (N'Doctor',           N'Gourav_Dev_Doctor',    N'Gourav', N'Doctor'),
    (N'Account',          N'Gourav_Dev_Account',   N'Gourav', N'Account'),
    (N'PharmacyPartner',  N'Gourav_Dev_Pharmacy',  N'Gourav', N'Pharmacy'),
    (N'Patient',          N'Gourav_Dev_Patient',   N'Gourav', N'Patient'),
    (N'Patient',          N'Gourav_Dev_Caregiver', N'Gourav', N'Caregiver'),
    (N'EmptyMenuProbe',   N'Gourav_Dev_NoMenu',    N'Gourav', N'NoMenu');

DECLARE @RoleName NVARCHAR(50), @UserName NVARCHAR(200),
        @First NVARCHAR(100), @Last NVARCHAR(100), @RoleId INT, @UserId BIGINT;

DECLARE u_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName, UserName, FirstName, LastName FROM @Users;

OPEN u_cursor;
FETCH NEXT FROM u_cursor INTO @RoleName, @UserName, @First, @Last;

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
        SET @UserId = (
            SELECT TOP 1 UserId FROM dbo.UserMaster
            WHERE ISNULL(DeleteStatus, 0) = 0 AND UserName = @UserName
        );

        IF @UserId IS NOT NULL
        BEGIN
            UPDATE dbo.UserMaster
            SET UserPassword = @Pwd,
                RoleId = @RoleId,
                EmailId = @Email,
                MobileNo = @Mobile,
                FirstName = @First,
                LastName = @Last,
                UserStatus = 1,
                IsUserActivated = 1,
                DeleteStatus = 0
            WHERE UserId = @UserId;
            PRINT CONCAT('UPDATED UserMaster ', @UserName, ' UserId=', @UserId, ' role=', @RoleName);
        END
        ELSE
        BEGIN
            INSERT INTO dbo.UserMaster (
                UserName, UserPassword, DeleteStatus, RoleId, UserStatus,
                MobileNo, EmailId, FirstName, LastName,
                CountryId, StateId, IsUserActivated, EnteredBy, EnteredDate
            )
            VALUES (
                @UserName, @Pwd, 0, @RoleId, 1,
                @Mobile, @Email, @First, @Last,
                78, 14, 1, @EnteredBy, GETDATE()
            );
            SET @UserId = SCOPE_IDENTITY();
            PRINT CONCAT('INSERTED UserMaster ', @UserName, ' UserId=', @UserId, ' role=', @RoleName);
        END
    END

    FETCH NEXT FROM u_cursor INTO @RoleName, @UserName, @First, @Last;
END

CLOSE u_cursor;
DEALLOCATE u_cursor;

/* Doctor row for Gourav_Dev_Doctor */
DECLARE @DocUserId BIGINT = (
    SELECT TOP 1 UserId FROM dbo.UserMaster
    WHERE UserName = N'Gourav_Dev_Doctor' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @DoctorId INT = NULL;

IF @DocUserId IS NULL
    PRINT 'SKIP Doctor: Gourav_Dev_Doctor UserMaster missing';
ELSE IF @DocUserId > 2147483647
    RAISERROR('Gourav_Dev_Doctor UserId does not fit Doctor.UserId INT.', 16, 1);
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
            N'Gourav', N'Doctor', @QualId, N'Gourav seed clinic',
            @Mobile, @Email,
            N'Pune', @EnteredBy, GETDATE(), 0, CAST(@DocUserId AS INT),
            N'Gourav Homeopathy Clinic', 550, 420,
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
            MobileNo = @Mobile,
            EmailId = @Email,
            DirectoryVisible = 1,
            VerificationStatus = N'Verified',
            PracticeActivated = 1,
            IsOnline = 1,
            DeleteStatus = 0,
            ConsultFeeInClinic = ISNULL(ConsultFeeInClinic, 550),
            ConsultFeeTele = ISNULL(ConsultFeeTele, 420),
            ClinicName = ISNULL(NULLIF(LTRIM(RTRIM(ClinicName)), N''), N'Gourav Homeopathy Clinic')
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
        PRINT 'INSERTED DoctorVerification Approved for Gourav_Dev_Doctor';
    END
END

/* Reception login */
IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NULL
    PRINT 'SKIP reception: DoctorReceptionStaff missing';
ELSE IF @DoctorId IS NULL
    PRINT 'SKIP reception: Gourav_Dev_Doctor DoctorId missing';
ELSE IF EXISTS (
    SELECT 1 FROM dbo.DoctorReceptionStaff
    WHERE UserID = N'Gourav_Dev_Reception' AND ISNULL(DeleteStatus, 0) = 0
)
BEGIN
    UPDATE dbo.DoctorReceptionStaff
    SET Password = N'123456',
        DoctorID = @DoctorId,
        ContactNumber = @Mobile,
        EmailId = @Email,
        FullName = N'Gourav Reception',
        IsActive = 1,
        DeleteStatus = 0
    WHERE UserID = N'Gourav_Dev_Reception';
    PRINT 'UPDATED DoctorReceptionStaff Gourav_Dev_Reception';
END
ELSE
BEGIN
    INSERT INTO dbo.DoctorReceptionStaff (
        DoctorID, UserID, Password, FullName, Address, ContactNumber, EmailId,
        Country, State, City, EnteredBy, EnteredDate, DeleteStatus, IsActive
    )
    VALUES (
        @DoctorId,
        N'Gourav_Dev_Reception',
        N'123456',
        N'Gourav Reception',
        N'Gourav seed clinic',
        @Mobile,
        @Email,
        N'India', N'Maharashtra', N'Pune',
        NULL, GETDATE(), 0, 1
    );
    PRINT 'INSERTED DoctorReceptionStaff Gourav_Dev_Reception';
END

/* Patient + map */
DECLARE @PatUserId BIGINT = (
    SELECT TOP 1 UserId FROM dbo.UserMaster
    WHERE UserName = N'Gourav_Dev_Patient' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @PatientId INT = NULL;

IF @PatUserId IS NULL
    PRINT 'SKIP Patient: Gourav_Dev_Patient UserMaster missing';
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
            N'Gourav Patient',
            N'Gourav seed',
            14, 78, @Mobile,
            '1990-01-01', 0, @Email,
            DATEDIFF(YEAR, '1990-01-01', GETDATE()),
            0,
            0, @EnteredBy, GETDATE()
        );
        SET @PatientId = SCOPE_IDENTITY();

        INSERT INTO dbo.PatientUserMap (UserId, PatientId, IsPrimary, DeleteStatus, EnteredBy, EnteredDate)
        VALUES (@PatUserId, @PatientId, 1, 0, @EnteredBy, GETUTCDATE());
        PRINT CONCAT('INSERTED Patient+Map PatientId=', @PatientId, ' UserId=', @PatUserId);
    END
    ELSE
    BEGIN
        UPDATE dbo.Patient
        SET MobileNo = @Mobile, Email = @Email
        WHERE PatientId = @PatientId;
        PRINT CONCAT('UPDATED Patient contact PatientId=', @PatientId);
    END
END

/* Caregiver grant */
DECLARE @CgUserId BIGINT = (
    SELECT TOP 1 UserId FROM dbo.UserMaster
    WHERE UserName = N'Gourav_Dev_Caregiver' AND ISNULL(DeleteStatus, 0) = 0
);

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
    PRINT 'INSERTED CaregiverAuthorization Gourav_Dev_Caregiver -> Gourav_Dev_Patient';
END

COMMIT TRAN;

PRINT '009_PROD_Seed_Gourav_Dev_Role_Logins.sql completed.';
PRINT 'Password for all: 123456';
PRINT 'UserMaster: Gourav_Dev_Admin / Doctor / Reception / Account / Pharmacy / Patient / Caregiver / NoMenu';
GO

/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 08_DEV_Seed_S1_NewTables_Sample_Tested.sql
Purpose      : Sample TESTED rows in every S1 Week 1 NEW table so APIs can be listed/queried.
Use          : Dev/test data only. Run after 01–05. Notes / tokens tagged S1-TESTED.
Prerequisites: 05 Patient login tufanpowar001@gmail.com + PatientUserMap.
Idempotent   : Yes. Skips existing S1-TESTED rows. Does not duplicate family/caregiver.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

DECLARE @Marker NVARCHAR(40) = N'S1-TESTED';
DECLARE @PatientLogin NVARCHAR(200) = N'tufanpowar001@gmail.com';
DECLARE @CaregiverLogin NVARCHAR(200) = N's1.caregiver.tested@homeocentrum.dev';
DECLARE @SpouseEmail NVARCHAR(200) = N's1.family.spouse.tested@homeocentrum.dev';
DECLARE @Pwd NVARCHAR(500) = N'PBKDF2$v1$100000$uszumO1aPL3it0x1tm/FcA==$hvFLmFwfXrC7i3Id07HFiH9ah4WiIAdjzYPyQnArLxQ='; -- 123456
DECLARE @OtpHash NVARCHAR(128) = CONVERT(VARCHAR(64), HASHBYTES(N'SHA2_256', CONVERT(VARBINARY(32), N'123456')), 2);
DECLARE @ResetHash NVARCHAR(128) = CONVERT(VARCHAR(64), HASHBYTES(N'SHA2_256', CONVERT(VARBINARY(64), N'S1-TESTED-RESET-TOKEN')), 2);
DECLARE @Now DATETIME = '2026-09-18T12:00:00';

IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Patient', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NULL
   OR OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ConsentRecord', N'U') IS NULL
   OR OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NULL
   OR OBJECT_ID(N'dbo.OtpAuditLog', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AuditEvent', N'U') IS NULL
   OR OBJECT_ID(N'dbo.SecureDocument', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PasswordResetToken', N'U') IS NULL
   OR OBJECT_ID(N'dbo.UserAppPreference', N'U') IS NULL
   OR OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NULL
BEGIN
    RAISERROR('Required S1 tables missing. Run scripts 01–05 first.', 16, 1);
    RETURN;
END

DECLARE @OwnerUserId BIGINT;
DECLARE @OwnerPatientId INT;
DECLARE @OwnerMobile NVARCHAR(20);

SELECT TOP 1
    @OwnerUserId = um.UserId,
    @OwnerPatientId = pum.PatientId,
    @OwnerMobile = um.MobileNo
FROM dbo.UserMaster um
INNER JOIN dbo.PatientUserMap pum
    ON pum.UserId = um.UserId AND pum.IsPrimary = 1 AND ISNULL(pum.DeleteStatus, 0) = 0
WHERE um.UserName = @PatientLogin
  AND ISNULL(um.DeleteStatus, 0) = 0;

IF @OwnerUserId IS NULL OR @OwnerPatientId IS NULL
BEGIN
    RAISERROR('Patient seed tufanpowar001@gmail.com / PatientUserMap missing. Run 05_DEV_Seed_Patient_Portal_TufanPowar.sql first.', 16, 1);
    RETURN;
END

IF @OwnerMobile IS NULL OR LTRIM(RTRIM(@OwnerMobile)) = N''
    SET @OwnerMobile = N'7768046064';

DECLARE @PatientRoleId INT = (
    SELECT TOP 1 RoleId FROM dbo.RoleMaster
    WHERE RoleName = N'Patient' AND ISNULL(DeleteStatus, 0) = 0
);

IF @PatientRoleId IS NULL
BEGIN
    RAISERROR('RoleMaster missing Patient.', 16, 1);
    RETURN;
END

DECLARE @LangId INT = NULL;
IF OBJECT_ID(N'dbo.LanguageMaster', N'U') IS NOT NULL
    SELECT TOP 1 @LangId = LanguageId FROM dbo.LanguageMaster ORDER BY LanguageId;

DECLARE @PrivacyTypeId INT = (SELECT TOP 1 ConsentTypeId FROM dbo.ConsentType WHERE Code = N'Privacy');
DECLARE @BookingTypeId INT = (SELECT TOP 1 ConsentTypeId FROM dbo.ConsentType WHERE Code = N'Booking');
DECLARE @CaregiverTypeId INT = (SELECT TOP 1 ConsentTypeId FROM dbo.ConsentType WHERE Code = N'Caregiver');

BEGIN TRAN;

-- ---------------------------------------------------------------------------
-- Caregiver UserMaster (login 123456) — tested
-- ---------------------------------------------------------------------------
DECLARE @CaregiverUserId BIGINT = (
    SELECT TOP 1 UserId FROM dbo.UserMaster
    WHERE UserName = @CaregiverLogin AND ISNULL(DeleteStatus, 0) = 0
);

IF @CaregiverUserId IS NULL
BEGIN
    INSERT INTO dbo.UserMaster (
        UserName, UserPassword, UserStatus, MobileNo, EmailId,
        FirstName, LastName, CountryId, StateId, RoleId,
        DeleteStatus, IsUserActivated, EnteredBy, EnteredDate
    )
    VALUES (
        @CaregiverLogin, @Pwd, 1, N'9000000002', @CaregiverLogin,
        N'S1', N'CaregiverTested', 78, 14, @PatientRoleId,
        0, 1, @Marker, GETDATE()
    );
    SET @CaregiverUserId = SCOPE_IDENTITY();
    PRINT 'INSERTED UserMaster caregiver S1-TESTED';
END
ELSE
    PRINT 'SKIP UserMaster caregiver already present';

-- ---------------------------------------------------------------------------
-- Family member Patient + PatientFamilyMember — tested
-- ---------------------------------------------------------------------------
DECLARE @MemberPatientId INT = (
    SELECT TOP 1 PatientId FROM dbo.Patient
    WHERE Email = @SpouseEmail AND ISNULL(DeleteStatus, 0) = 0
);

IF @MemberPatientId IS NULL
BEGIN
    IF COL_LENGTH(N'dbo.Patient', N'IsWhatsAppOptIn') IS NOT NULL
        INSERT INTO dbo.Patient (
            PatientName, Address, StateId, CountryId, MobileNo, PhoneNo,
            DateOfBirth, Gender, Email, Age, IsWhatsAppOptIn, WhatsAppOptInDate,
            DeleteStatus, EnteredBy, EnteredDate
        )
        VALUES (
            N'Anita Powar (S1-TESTED)', N'S1 test family member', 14, 78, N'9000000003', NULL,
            '1992-05-01', 0, @SpouseEmail, DATEDIFF(YEAR, '1992-05-01', GETDATE()),
            0, NULL, 0, @Marker, GETDATE()
        );
    ELSE
        INSERT INTO dbo.Patient (
            PatientName, Address, StateId, CountryId, MobileNo, PhoneNo,
            DateOfBirth, Gender, Email, Age, DeleteStatus, EnteredBy, EnteredDate
        )
        VALUES (
            N'Anita Powar (S1-TESTED)', N'S1 test family member', 14, 78, N'9000000003', NULL,
            '1992-05-01', 0, @SpouseEmail, DATEDIFF(YEAR, '1992-05-01', GETDATE()),
            0, @Marker, GETDATE()
        );

    SET @MemberPatientId = SCOPE_IDENTITY();
    PRINT 'INSERTED Patient family member S1-TESTED';
END
ELSE
    PRINT 'SKIP Patient family member already present';

IF NOT EXISTS (
    SELECT 1 FROM dbo.PatientFamilyMember
    WHERE OwnerUserId = @OwnerUserId
      AND MemberPatientId = @MemberPatientId
      AND ISNULL(DeleteStatus, 0) = 0
)
BEGIN
    INSERT INTO dbo.PatientFamilyMember (
        OwnerUserId, OwnerPatientId, MemberPatientId, Relation,
        DeleteStatus, EnteredBy, EnteredDate
    )
    VALUES (
        @OwnerUserId, @OwnerPatientId, @MemberPatientId, N'Spouse',
        0, @Marker, @Now
    );
    PRINT 'INSERTED PatientFamilyMember S1-TESTED';
END
ELSE
    PRINT 'SKIP PatientFamilyMember already present';

-- ---------------------------------------------------------------------------
-- CaregiverAuthorization + Caregiver consent — tested
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM dbo.CaregiverAuthorization
    WHERE PatientId = @OwnerPatientId
      AND CaregiverUserId = @CaregiverUserId
      AND ISNULL(DeleteStatus, 0) = 0
      AND RevokedAt IS NULL
)
BEGIN
    INSERT INTO dbo.CaregiverAuthorization (
        PatientId, CaregiverUserId, GrantedByUserId, GrantedAt, RevokedAt, Scope, DeleteStatus
    )
    VALUES (
        @OwnerPatientId, @CaregiverUserId, @OwnerUserId, @Now, NULL, N'booking', 0
    );
    PRINT 'INSERTED CaregiverAuthorization S1-TESTED';
END
ELSE
    PRINT 'SKIP CaregiverAuthorization already present';

IF @CaregiverTypeId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.ConsentRecord
        WHERE Notes = N'S1-TESTED Caregiver'
          AND SubjectType = N'Patient'
          AND SubjectId = @OwnerPatientId
   )
BEGIN
    INSERT INTO dbo.ConsentRecord (
        ConsentTypeId, SubjectType, SubjectId, GrantedByUserId, GrantedAt,
        IpAddress, UserAgent, Notes
    )
    VALUES (
        @CaregiverTypeId, N'Patient', @OwnerPatientId, @OwnerUserId, @Now,
        N'127.0.0.1', N'S1-TESTED sql seed', N'S1-TESTED Caregiver'
    );
    PRINT 'INSERTED ConsentRecord Caregiver S1-TESTED';
END

IF @PrivacyTypeId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.ConsentRecord
        WHERE Notes = N'S1-TESTED Privacy'
          AND SubjectType = N'User'
          AND SubjectId = @OwnerUserId
   )
BEGIN
    INSERT INTO dbo.ConsentRecord (
        ConsentTypeId, SubjectType, SubjectId, GrantedByUserId, GrantedAt,
        IpAddress, UserAgent, Notes
    )
    VALUES (
        @PrivacyTypeId, N'User', @OwnerUserId, @OwnerUserId, @Now,
        N'127.0.0.1', N'S1-TESTED sql seed', N'S1-TESTED Privacy'
    );
    PRINT 'INSERTED ConsentRecord Privacy S1-TESTED';
END

IF @BookingTypeId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.ConsentRecord
        WHERE Notes = N'S1-TESTED Booking'
          AND SubjectType = N'User'
          AND SubjectId = @OwnerUserId
   )
BEGIN
    INSERT INTO dbo.ConsentRecord (
        ConsentTypeId, SubjectType, SubjectId, GrantedByUserId, GrantedAt,
        IpAddress, UserAgent, Notes
    )
    VALUES (
        @BookingTypeId, N'User', @OwnerUserId, @OwnerUserId, @Now,
        N'127.0.0.1', N'S1-TESTED sql seed', N'S1-TESTED Booking'
    );
    PRINT 'INSERTED ConsentRecord Booking S1-TESTED';
END

-- ---------------------------------------------------------------------------
-- UserAppPreference — tested
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.UserAppPreference WHERE UserId = @OwnerUserId)
BEGIN
    INSERT INTO dbo.UserAppPreference (UserId, PreferredLanguageId, WelcomeVersionSeen, UpdatedAt)
    VALUES (@OwnerUserId, @LangId, N'1', @Now);
    PRINT 'INSERTED UserAppPreference S1-TESTED';
END
ELSE
BEGIN
    UPDATE dbo.UserAppPreference
    SET PreferredLanguageId = COALESCE(PreferredLanguageId, @LangId),
        WelcomeVersionSeen = COALESCE(WelcomeVersionSeen, N'1'),
        UpdatedAt = @Now
    WHERE UserId = @OwnerUserId;
    PRINT 'UPDATED UserAppPreference S1-TESTED';
END

-- ---------------------------------------------------------------------------
-- DevicePushToken — tested
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM dbo.DevicePushToken
    WHERE UserId = @OwnerUserId AND Token = N'S1-TESTED-FCM-TOKEN' AND DeleteStatus = 0
)
BEGIN
    INSERT INTO dbo.DevicePushToken (UserId, Platform, Token, DeviceId, CreatedAt, UpdatedAt, DeleteStatus)
    VALUES (@OwnerUserId, N'FCM', N'S1-TESTED-FCM-TOKEN', N's1-tested-emu-1', @Now, @Now, 0);
    PRINT 'INSERTED DevicePushToken S1-TESTED';
END
ELSE
    PRINT 'SKIP DevicePushToken already present';

-- ---------------------------------------------------------------------------
-- OtpChallenge + OtpAuditLog — already verified (historical tested)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM dbo.OtpChallenge
    WHERE Action = N'Login' AND EntityType = N'Mobile' AND EntityId = @OwnerMobile
      AND DestinationMasked LIKE N'S1-TESTED%'
)
BEGIN
    INSERT INTO dbo.OtpChallenge (
        Action, EntityType, EntityId, DestinationMasked, OtpHash,
        ExpiresAt, AttemptCount, LockedUntil, CreatedAt, VerifiedAt
    )
    VALUES (
        N'Login', N'Mobile', @OwnerMobile, N'S1-TESTED ****6064', @OtpHash,
        DATEADD(MINUTE, -1, @Now), 1, NULL, DATEADD(MINUTE, -11, @Now), DATEADD(MINUTE, -10, @Now)
    );
    PRINT 'INSERTED OtpChallenge Login S1-TESTED (verified)';
END
ELSE
    PRINT 'SKIP OtpChallenge Login already present';

IF NOT EXISTS (
    SELECT 1 FROM dbo.OtpChallenge
    WHERE Action = N'GrantCaregiver' AND EntityType = N'Patient'
      AND EntityId = CONVERT(NVARCHAR(20), @OwnerPatientId)
      AND DestinationMasked LIKE N'S1-TESTED%'
)
BEGIN
    INSERT INTO dbo.OtpChallenge (
        Action, EntityType, EntityId, DestinationMasked, OtpHash,
        ExpiresAt, AttemptCount, LockedUntil, CreatedAt, VerifiedAt
    )
    VALUES (
        N'GrantCaregiver', N'Patient', CONVERT(NVARCHAR(20), @OwnerPatientId), N'S1-TESTED ****6064', @OtpHash,
        DATEADD(MINUTE, -1, @Now), 1, NULL, DATEADD(MINUTE, -11, @Now), DATEADD(MINUTE, -10, @Now)
    );
    PRINT 'INSERTED OtpChallenge GrantCaregiver S1-TESTED (verified)';
END
ELSE
    PRINT 'SKIP OtpChallenge GrantCaregiver already present';

IF NOT EXISTS (SELECT 1 FROM dbo.OtpAuditLog WHERE ToMasked = N'S1-TESTED ****6064' AND Action = N'RequestOtp')
BEGIN
    INSERT INTO dbo.OtpAuditLog (Action, EntityType, EntityId, ToMasked, Success, At, ActorUserId)
    VALUES
        (N'RequestOtp', N'Mobile', @OwnerMobile, N'S1-TESTED ****6064', 1, DATEADD(MINUTE, -11, @Now), NULL),
        (N'VerifyOtp', N'Mobile', @OwnerMobile, N'S1-TESTED ****6064', 1, DATEADD(MINUTE, -10, @Now), @OwnerUserId);
    PRINT 'INSERTED OtpAuditLog S1-TESTED';
END
ELSE
    PRINT 'SKIP OtpAuditLog already present';

-- ---------------------------------------------------------------------------
-- AuditEvent — tested
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AuditEvent WHERE CorrelationId = N'S1-TESTED')
BEGIN
    INSERT INTO dbo.AuditEvent (ActorUserId, Role, Action, Entity, OldJson, NewJson, At, CorrelationId)
    VALUES (
        @OwnerUserId, N'Patient', N'SampleSeed', N'S1Week1',
        NULL, N'{"marker":"S1-TESTED","result":"tested"}', @Now, N'S1-TESTED'
    );
    PRINT 'INSERTED AuditEvent S1-TESTED';
END
ELSE
    PRINT 'SKIP AuditEvent already present';

-- ---------------------------------------------------------------------------
-- SecureDocument — tested
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.SecureDocument WHERE BlobPath = N'attachments/s1-tested/sample.pdf')
BEGIN
    INSERT INTO dbo.SecureDocument (OwnerType, OwnerId, BlobPath, FileName, Mime, Hash, CreatedBy, CreatedAt)
    VALUES (
        N'Patient', @OwnerPatientId, N'attachments/s1-tested/sample.pdf',
        N'sample.pdf', N'application/pdf', N'S1-TESTED-HASH', @OwnerUserId, @Now
    );
    PRINT 'INSERTED SecureDocument S1-TESTED';
END
ELSE
    PRINT 'SKIP SecureDocument already present';

-- ---------------------------------------------------------------------------
-- PasswordResetToken — used / expired tested row
-- ---------------------------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM dbo.PasswordResetToken
    WHERE UserId = @OwnerUserId AND TokenHash = @ResetHash
)
BEGIN
    INSERT INTO dbo.PasswordResetToken (UserId, TokenHash, ExpiresAt, UsedAt, CreatedAt)
    VALUES (
        @OwnerUserId, @ResetHash, DATEADD(HOUR, -1, @Now), DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -3, @Now)
    );
    PRINT 'INSERTED PasswordResetToken S1-TESTED (used)';
END
ELSE
    PRINT 'SKIP PasswordResetToken already present';

COMMIT TRAN;

PRINT '08_DEV_Seed_S1_NewTables_Sample_Tested.sql completed.';
PRINT 'Test logins: tufanpowar001@gmail.com / 123456  and  s1.caregiver.tested@homeocentrum.dev / 123456';

SELECT
    @OwnerUserId AS OwnerUserId,
    @OwnerPatientId AS OwnerPatientId,
    @MemberPatientId AS FamilyMemberPatientId,
    @CaregiverUserId AS CaregiverUserId,
    @Marker AS Marker;

SELECT N'ConsentRecord' AS SampleTable, COUNT(*) AS TestedRows FROM dbo.ConsentRecord WHERE Notes LIKE N'S1-TESTED%'
UNION ALL SELECT N'PatientFamilyMember', COUNT(*) FROM dbo.PatientFamilyMember WHERE EnteredBy = @Marker
UNION ALL SELECT N'CaregiverAuthorization', COUNT(*) FROM dbo.CaregiverAuthorization
    WHERE PatientId = @OwnerPatientId AND CaregiverUserId = @CaregiverUserId AND ISNULL(DeleteStatus, 0) = 0
UNION ALL SELECT N'UserAppPreference', COUNT(*) FROM dbo.UserAppPreference WHERE UserId = @OwnerUserId
UNION ALL SELECT N'DevicePushToken', COUNT(*) FROM dbo.DevicePushToken WHERE Token = N'S1-TESTED-FCM-TOKEN'
UNION ALL SELECT N'OtpChallenge', COUNT(*) FROM dbo.OtpChallenge WHERE DestinationMasked LIKE N'S1-TESTED%'
UNION ALL SELECT N'OtpAuditLog', COUNT(*) FROM dbo.OtpAuditLog WHERE ToMasked = N'S1-TESTED ****6064'
UNION ALL SELECT N'AuditEvent', COUNT(*) FROM dbo.AuditEvent WHERE CorrelationId = N'S1-TESTED'
UNION ALL SELECT N'SecureDocument', COUNT(*) FROM dbo.SecureDocument WHERE BlobPath = N'attachments/s1-tested/sample.pdf'
UNION ALL SELECT N'PasswordResetToken', COUNT(*) FROM dbo.PasswordResetToken WHERE TokenHash = @ResetHash;
GO

/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : Test sample data insert.sql
Purpose      : Insert 10 TEST sample rows into each S1 Week 1 NEW table.
Use          : TEST / UAT / local Dev only.
DO NOT RUN ON PRODUCTION (creates fake patients, users, OTP, tokens).
Prerequisites: Scripts 01–05. Owner login tufanpowar001@gmail.com + PatientUserMap.
Idempotent   : Yes. Uses emails / tokens / notes tagged S1-SAMPLE-01 .. S1-SAMPLE-10.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

DECLARE @OwnerLogin NVARCHAR(200) = N'tufanpowar001@gmail.com';
DECLARE @Pwd NVARCHAR(500) = N'PBKDF2$v1$100000$uszumO1aPL3it0x1tm/FcA==$hvFLmFwfXrC7i3Id07HFiH9ah4WiIAdjzYPyQnArLxQ='; -- 123456
DECLARE @OtpHash NVARCHAR(128) = CONVERT(VARCHAR(64), HASHBYTES(N'SHA2_256', CONVERT(VARBINARY(32), N'123456')), 2);
DECLARE @Now DATETIME = '2026-09-18T12:00:00';
DECLARE @Marker NVARCHAR(40) = N'S1-SAMPLE';

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
    RAISERROR('Required S1 tables missing. Run Database scripts 01-05 first.', 16, 1);
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
WHERE um.UserName = @OwnerLogin
  AND ISNULL(um.DeleteStatus, 0) = 0;

IF @OwnerUserId IS NULL OR @OwnerPatientId IS NULL
BEGIN
    RAISERROR('Owner tufanpowar001@gmail.com not found. Run 05_DEV_Seed_Patient_Portal_TufanPowar.sql first.', 16, 1);
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
DECLARE @HasWhatsApp BIT = CASE WHEN COL_LENGTH(N'dbo.Patient', N'IsWhatsAppOptIn') IS NOT NULL THEN 1 ELSE 0 END;

DECLARE @Relations TABLE (N INT PRIMARY KEY, Relation NVARCHAR(50), FirstName NVARCHAR(50));
INSERT INTO @Relations (N, Relation, FirstName) VALUES
 (1, N'Spouse', N'Anita'),
 (2, N'Child', N'Rohan'),
 (3, N'Child', N'Sara'),
 (4, N'Parent', N'Suresh'),
 (5, N'Parent', N'Lata'),
 (6, N'Sibling', N'Amit'),
 (7, N'Sibling', N'Neha'),
 (8, N'Other', N'Kiran'),
 (9, N'Other', N'Meera'),
 (10, N'Other', N'Arjun');

BEGIN TRAN;

DECLARE @i INT = 1;
DECLARE @Pad CHAR(2);
DECLARE @Tag NVARCHAR(40);
DECLARE @FamilyEmail NVARCHAR(200);
DECLARE @CgEmail NVARCHAR(200);
DECLARE @FamilyMobile NVARCHAR(20);
DECLARE @CgMobile NVARCHAR(20);
DECLARE @Rel NVARCHAR(50);
DECLARE @FName NVARCHAR(50);
DECLARE @CaregiverUserId BIGINT;
DECLARE @MemberPatientId INT;
DECLARE @ResetHash NVARCHAR(128);

WHILE @i <= 10
BEGIN
    SET @Pad = RIGHT('0' + CONVERT(VARCHAR(2), @i), 2);
    SET @Tag = N'S1-SAMPLE-' + @Pad;
    SET @FamilyEmail = N'tufanpowar001@gmail.com';
    SET @CgEmail = N'Tufan_Caregiver';
    SET @FamilyMobile = N'7768046064';
    SET @CgMobile = N'7768046064';
    SELECT @Rel = Relation, @FName = FirstName FROM @Relations WHERE N = @i;

    SET @CaregiverUserId = (
        SELECT TOP 1 UserId FROM dbo.UserMaster
        WHERE UserName = @CgEmail AND ISNULL(DeleteStatus, 0) = 0
    );
    IF @CaregiverUserId IS NULL
    BEGIN
        INSERT INTO dbo.UserMaster (
            UserName, UserPassword, UserStatus, MobileNo, EmailId,
            FirstName, LastName, CountryId, StateId, RoleId,
            DeleteStatus, IsUserActivated, EnteredBy, EnteredDate
        )
        VALUES (
            @CgEmail, @Pwd, 1, @CgMobile, N'tufanpowar001@gmail.com',
            @FName, N'SampleCg', 78, 14, @PatientRoleId,
            0, 1, @Marker, GETDATE()
        );
        SET @CaregiverUserId = SCOPE_IDENTITY();
    END

    SET @MemberPatientId = (
        SELECT TOP 1 PatientId FROM dbo.Patient
        WHERE PatientName = @FName + N' Sample ' + @Pad AND ISNULL(DeleteStatus, 0) = 0
    );
    IF @MemberPatientId IS NULL
    BEGIN
        IF @HasWhatsApp = 1
            INSERT INTO dbo.Patient (
                PatientName, Address, StateId, CountryId, MobileNo, PhoneNo,
                DateOfBirth, Gender, Email, Age, IsWhatsAppOptIn, WhatsAppOptInDate,
                DeleteStatus, EnteredBy, EnteredDate
            )
            VALUES (
                @FName + N' Sample ' + @Pad, N'S1 sample family row ' + @Pad, 14, 78, @FamilyMobile, NULL,
                DATEADD(YEAR, -20 - @i, '2000-01-01'), CASE WHEN @i % 2 = 0 THEN 0 ELSE 1 END,
                @FamilyEmail, 20 + @i, 0, NULL, 0, @Marker, GETDATE()
            );
        ELSE
            INSERT INTO dbo.Patient (
                PatientName, Address, StateId, CountryId, MobileNo, PhoneNo,
                DateOfBirth, Gender, Email, Age, DeleteStatus, EnteredBy, EnteredDate
            )
            VALUES (
                @FName + N' Sample ' + @Pad, N'S1 sample family row ' + @Pad, 14, 78, @FamilyMobile, NULL,
                DATEADD(YEAR, -20 - @i, '2000-01-01'), CASE WHEN @i % 2 = 0 THEN 0 ELSE 1 END,
                @FamilyEmail, 20 + @i, 0, @Marker, GETDATE()
            );
        SET @MemberPatientId = SCOPE_IDENTITY();
    END

    IF NOT EXISTS (
        SELECT 1 FROM dbo.PatientFamilyMember
        WHERE OwnerUserId = @OwnerUserId AND MemberPatientId = @MemberPatientId AND ISNULL(DeleteStatus, 0) = 0
    )
        INSERT INTO dbo.PatientFamilyMember (
            OwnerUserId, OwnerPatientId, MemberPatientId, Relation,
            DeleteStatus, EnteredBy, EnteredDate
        )
        VALUES (@OwnerUserId, @OwnerPatientId, @MemberPatientId, @Rel, 0, @Tag, @Now);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.CaregiverAuthorization
        WHERE PatientId = @OwnerPatientId AND CaregiverUserId = @CaregiverUserId AND ISNULL(DeleteStatus, 0) = 0
    )
        INSERT INTO dbo.CaregiverAuthorization (
            PatientId, CaregiverUserId, GrantedByUserId, GrantedAt, RevokedAt, Scope, DeleteStatus
        )
        VALUES (
            @OwnerPatientId, @CaregiverUserId, @OwnerUserId, DATEADD(MINUTE, -@i, @Now),
            CASE WHEN @i = 10 THEN @Now ELSE NULL END,
            N'booking', 0
        );

    IF @PrivacyTypeId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.ConsentRecord WHERE Notes = @Tag + N' Privacy')
        INSERT INTO dbo.ConsentRecord (
            ConsentTypeId, SubjectType, SubjectId, GrantedByUserId, GrantedAt,
            IpAddress, UserAgent, Notes
        )
        VALUES (
            CASE @i % 3
                WHEN 0 THEN COALESCE(@CaregiverTypeId, @PrivacyTypeId)
                WHEN 1 THEN @PrivacyTypeId
                ELSE COALESCE(@BookingTypeId, @PrivacyTypeId)
            END,
            CASE WHEN @i % 3 = 0 THEN N'Patient' ELSE N'User' END,
            CASE WHEN @i % 3 = 0 THEN @OwnerPatientId ELSE @OwnerUserId END,
            @OwnerUserId, DATEADD(MINUTE, -@i, @Now),
            N'127.0.0.1', N'Test sample data insert.sql', @Tag + N' Privacy'
        );

    IF NOT EXISTS (SELECT 1 FROM dbo.UserAppPreference WHERE UserId = @CaregiverUserId)
        INSERT INTO dbo.UserAppPreference (UserId, PreferredLanguageId, WelcomeVersionSeen, UpdatedAt)
        VALUES (@CaregiverUserId, @LangId, N'1', @Now);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.DevicePushToken
        WHERE Token = N'S1-SAMPLE-FCM-' + @Pad AND DeleteStatus = 0
    )
        INSERT INTO dbo.DevicePushToken (UserId, Platform, Token, DeviceId, CreatedAt, UpdatedAt, DeleteStatus)
        VALUES (
            CASE WHEN @i <= 5 THEN @OwnerUserId ELSE @CaregiverUserId END,
            CASE WHEN @i % 2 = 0 THEN N'APNs' ELSE N'FCM' END,
            N'S1-SAMPLE-FCM-' + @Pad,
            N's1-sample-device-' + @Pad,
            DATEADD(MINUTE, -@i, @Now), @Now, 0
        );

    IF NOT EXISTS (
        SELECT 1 FROM dbo.OtpChallenge
        WHERE DestinationMasked = @Tag AND Action = N'Login'
    )
        INSERT INTO dbo.OtpChallenge (
            Action, EntityType, EntityId, DestinationMasked, OtpHash,
            ExpiresAt, AttemptCount, LockedUntil, CreatedAt, VerifiedAt
        )
        VALUES (
            CASE WHEN @i % 2 = 1 THEN N'Login' ELSE N'GrantCaregiver' END,
            CASE WHEN @i % 2 = 1 THEN N'Mobile' ELSE N'Patient' END,
            CASE WHEN @i % 2 = 1 THEN @OwnerMobile ELSE CONVERT(NVARCHAR(20), @OwnerPatientId) END,
            @Tag, @OtpHash,
            DATEADD(MINUTE, -1, @Now), 1, NULL,
            DATEADD(MINUTE, -20 - @i, @Now), DATEADD(MINUTE, -15 - @i, @Now)
        );

    IF NOT EXISTS (SELECT 1 FROM dbo.OtpAuditLog WHERE ToMasked = @Tag)
        INSERT INTO dbo.OtpAuditLog (Action, EntityType, EntityId, ToMasked, Success, At, ActorUserId)
        VALUES (
            CASE WHEN @i % 2 = 1 THEN N'RequestOtp' ELSE N'VerifyOtp' END,
            N'Mobile', @OwnerMobile, @Tag, 1, DATEADD(MINUTE, -@i, @Now), @OwnerUserId
        );

    IF NOT EXISTS (SELECT 1 FROM dbo.AuditEvent WHERE CorrelationId = @Tag)
        INSERT INTO dbo.AuditEvent (ActorUserId, Role, Action, Entity, OldJson, NewJson, At, CorrelationId)
        VALUES (
            @OwnerUserId, N'Patient', N'SampleSeed', N'S1Week1',
            NULL,
            N'{"marker":"' + @Tag + N'","row":' + CONVERT(NVARCHAR(2), @i) + N'}',
            DATEADD(MINUTE, -@i, @Now), @Tag
        );

    IF NOT EXISTS (SELECT 1 FROM dbo.SecureDocument WHERE BlobPath = N'attachments/s1-sample/row-' + @Pad + N'.pdf')
        INSERT INTO dbo.SecureDocument (OwnerType, OwnerId, BlobPath, FileName, Mime, Hash, CreatedBy, CreatedAt)
        VALUES (
            N'Patient', @OwnerPatientId,
            N'attachments/s1-sample/row-' + @Pad + N'.pdf',
            N'sample-' + @Pad + N'.pdf', N'application/pdf',
            N'S1-SAMPLE-HASH-' + @Pad, @OwnerUserId, DATEADD(MINUTE, -@i, @Now)
        );

    SET @ResetHash = CONVERT(VARCHAR(64), HASHBYTES(N'SHA2_256', CONVERT(VARBINARY(64), @Tag)), 2);
    IF NOT EXISTS (
        SELECT 1 FROM dbo.PasswordResetToken
        WHERE UserId = @OwnerUserId AND TokenHash = @ResetHash
    )
        INSERT INTO dbo.PasswordResetToken (UserId, TokenHash, ExpiresAt, UsedAt, CreatedAt)
        VALUES (
            @OwnerUserId,
            @ResetHash,
            DATEADD(HOUR, -1, @Now),
            CASE WHEN @i <= 8 THEN DATEADD(HOUR, -2, @Now) ELSE NULL END,
            DATEADD(HOUR, -3, @Now)
        );

    SET @i += 1;
END

COMMIT TRAN;

PRINT 'Test sample data insert.sql completed (10 rows per new S1 table).';
PRINT 'TEST/UAT only. Do not run on production.';

SELECT N'PatientFamilyMember' AS SampleTable, COUNT(*) AS Rows10 FROM dbo.PatientFamilyMember WHERE EnteredBy LIKE N'S1-SAMPLE-%'
UNION ALL SELECT N'CaregiverAuthorization', COUNT(*) FROM dbo.CaregiverAuthorization ca
    INNER JOIN dbo.UserMaster um ON um.UserId = ca.CaregiverUserId
    WHERE um.UserName = N'Tufan_Caregiver'
UNION ALL SELECT N'ConsentRecord', COUNT(*) FROM dbo.ConsentRecord WHERE Notes LIKE N'S1-SAMPLE-%'
UNION ALL SELECT N'UserAppPreference', COUNT(*) FROM dbo.UserAppPreference p
    INNER JOIN dbo.UserMaster um ON um.UserId = p.UserId
    WHERE um.UserName = N'Tufan_Caregiver'
UNION ALL SELECT N'DevicePushToken', COUNT(*) FROM dbo.DevicePushToken WHERE Token LIKE N'S1-SAMPLE-FCM-%'
UNION ALL SELECT N'OtpChallenge', COUNT(*) FROM dbo.OtpChallenge WHERE DestinationMasked LIKE N'S1-SAMPLE-%'
UNION ALL SELECT N'OtpAuditLog', COUNT(*) FROM dbo.OtpAuditLog WHERE ToMasked LIKE N'S1-SAMPLE-%'
UNION ALL SELECT N'AuditEvent', COUNT(*) FROM dbo.AuditEvent WHERE CorrelationId LIKE N'S1-SAMPLE-%'
UNION ALL SELECT N'SecureDocument', COUNT(*) FROM dbo.SecureDocument WHERE BlobPath LIKE N'attachments/s1-sample/%'
UNION ALL SELECT N'PasswordResetToken', COUNT(*) FROM dbo.PasswordResetToken pr
    WHERE pr.UserId = @OwnerUserId AND pr.CreatedAt = DATEADD(HOUR, -3, @Now);
GO

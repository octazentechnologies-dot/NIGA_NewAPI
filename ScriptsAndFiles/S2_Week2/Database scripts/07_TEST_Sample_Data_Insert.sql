/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 07_TEST_Sample_Data_Insert.sql
Purpose      : Test sample data (10 rows per new Week-2 store) so Public / booking /
               enquiry / COG / profile KYC / hotspot APIs return rows on Dev.
Use          : Dev/test only. Run after 01–06 on HomeoCentrum_Dev.
Marker       : S2-TESTED (booking tokens S2TESTED01–10, enquiry emails s2.enquiry.*)
Idempotent   : Yes. Skips rows that already have the marker.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

DECLARE @Marker NVARCHAR(40) = N'S2-TESTED';
DECLARE @SeedDate DATE = '20260918';

IF OBJECT_ID(N'dbo.Doctor', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
   OR OBJECT_ID(N'dbo.EnquiryDetails', N'U') IS NULL
   OR OBJECT_ID(N'dbo.CogRun', N'U') IS NULL
   OR OBJECT_ID(N'dbo.DoctorPayeeKyc', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PolicyVersion', N'U') IS NULL
BEGIN
    RAISERROR('S2 Week 2 tables missing. Run scripts 01–06 first.', 16, 1);
    RETURN;
END

DECLARE @DoctorId INT;
DECLARE @DoctorUserId BIGINT;
DECLARE @PatientId INT;

SELECT TOP 1
    @DoctorId = d.DoctorId,
    @DoctorUserId = d.UserId
FROM dbo.Doctor d
INNER JOIN dbo.UserMaster um ON um.UserId = d.UserId
WHERE ISNULL(d.DeleteStatus, 0) = 0
  AND ISNULL(um.DeleteStatus, 0) = 0
  AND um.UserName IN (N'Tufan_Doctor', N'NIGA HOMEOPATHY')
ORDER BY CASE WHEN um.UserName = N'Tufan_Doctor' THEN 0 ELSE 1 END;

IF @DoctorId IS NULL
    SELECT TOP 1
        @DoctorId = d.DoctorId,
        @DoctorUserId = d.UserId
    FROM dbo.Doctor d
    WHERE ISNULL(d.DeleteStatus, 0) = 0
      AND d.DirectoryVisible = 1
      AND d.VerificationStatus = N'Verified'
    ORDER BY d.DoctorId;

SELECT TOP 1 @PatientId = m.PatientId
FROM dbo.PatientUserMap m
INNER JOIN dbo.UserMaster um ON um.UserId = m.UserId
WHERE ISNULL(m.DeleteStatus, 0) = 0
  AND ISNULL(um.DeleteStatus, 0) = 0
  AND um.UserName IN (N'Tufan_Patient', N'tufanpowar001@gmail.com')
ORDER BY CASE WHEN um.UserName = N'Tufan_Patient' THEN 0 ELSE 1 END;

IF @PatientId IS NULL
    SELECT TOP 1 @PatientId = p.PatientID
    FROM dbo.Patient p
    WHERE ISNULL(p.DeleteStatus, 0) = 0
    ORDER BY p.PatientID;

IF @DoctorId IS NULL OR @DoctorUserId IS NULL OR @PatientId IS NULL
BEGIN
    RAISERROR('Need a Verified directory doctor and a Patient row. Stop.', 16, 1);
    RETURN;
END

BEGIN TRAN;

-- ---------------------------------------------------------------------------
-- 10 doctors directory fields (fees / city / hours / some online)
-- ---------------------------------------------------------------------------
;WITH d AS (
    SELECT DoctorId, ROW_NUMBER() OVER (ORDER BY DoctorId) AS rn
    FROM dbo.Doctor
    WHERE ISNULL(DeleteStatus, 0) = 0
)
UPDATE doc
SET ConsultFeeInClinic = COALESCE(doc.ConsultFeeInClinic, 400 + (d.rn * 50)),
    ConsultFeeTele = COALESCE(doc.ConsultFeeTele, 300 + (d.rn * 40)),
    WorkingHoursNote = COALESCE(NULLIF(LTRIM(RTRIM(doc.WorkingHoursNote)), N''), N'Mon-Sat 10:00-18:00'),
    City = COALESCE(NULLIF(LTRIM(RTRIM(doc.City)), N''), N'Pune'),
    ClinicName = COALESCE(NULLIF(LTRIM(RTRIM(doc.ClinicName)), N''), LTRIM(RTRIM(ISNULL(doc.FirstName, N'') + N' ' + ISNULL(doc.LastName, N'')))),
    IsOnline = CASE WHEN d.rn <= 3 THEN 1 ELSE doc.IsOnline END,
    DirectoryVisible = 1,
    VerificationStatus = N'Verified',
    PracticeActivated = 1
FROM dbo.Doctor doc
INNER JOIN d ON d.DoctorId = doc.DoctorId;

PRINT 'UPDATED Doctor fees/city/hours/online for directory sample';

-- ---------------------------------------------------------------------------
-- DoctorPayeeKyc — one sample row per doctor (unique DoctorId)
-- ---------------------------------------------------------------------------
INSERT INTO dbo.DoctorPayeeKyc (DoctorId, AccountHolder, BankName, AccountNumber, Ifsc, Pan, CreatedAt, DeleteStatus)
SELECT
    d.DoctorId,
    LEFT(LTRIM(RTRIM(ISNULL(d.FirstName, N'Doctor') + N' ' + ISNULL(d.LastName, N''))), 200),
    N'S2-TESTED Bank',
    N'XXXX' + RIGHT(N'000000' + CONVERT(VARCHAR(12), d.DoctorId), 6),
    N'HDFC0001234',
    N'ABCDE1234F',
    GETUTCDATE(),
    0
FROM dbo.Doctor d
WHERE ISNULL(d.DeleteStatus, 0) = 0
  AND NOT EXISTS (
        SELECT 1 FROM dbo.DoctorPayeeKyc k
        WHERE k.DoctorId = d.DoctorId AND ISNULL(k.DeleteStatus, 0) = 0
    );
DECLARE @KycCnt INT = (SELECT COUNT(*) FROM dbo.DoctorPayeeKyc WHERE ISNULL(DeleteStatus, 0) = 0);
PRINT CONCAT('DoctorPayeeKyc sample rows now = ', @KycCnt);

-- ---------------------------------------------------------------------------
-- PolicyVersion — Booking (Privacy/Terms already in 02)
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.PolicyVersion WHERE PolicyType = N'Booking' AND Version = N'2026.09')
BEGIN
    INSERT INTO dbo.PolicyVersion (PolicyType, Version, Title, BodyHtml, IsCurrent)
    VALUES (N'Booking', N'2026.09', N'Booking consent', N'<p>S2-TESTED booking consent. Recording and pharmacy share require explicit consent. Version 2026.09.</p>', 1);
    PRINT 'INSERTED PolicyVersion Booking 2026.09';
END
ELSE
    PRINT 'SKIP PolicyVersion Booking already present';

-- ---------------------------------------------------------------------------
-- 10 EnquiryDetails
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.EnquiryDetails WHERE EnquiryName LIKE N'S2 Guest %')
BEGIN
    INSERT INTO dbo.EnquiryDetails (EnquiryName, EnquiryDate, EmailId, MobileNo, EnquiryDetails, EnquiryStatus, TicketStatus, AssignedTo)
    VALUES
        (N'S2 Guest 01', DATEADD(DAY,  0, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: looking for a doctor in Pune', 1, N'New',    NULL),
        (N'S2 Guest 02', DATEADD(DAY,  0, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: tele consult fees', 1, N'New',    @DoctorUserId),
        (N'S2 Guest 03', DATEADD(DAY, -1, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: clinic hours', 1, N'Open',   @DoctorUserId),
        (N'S2 Guest 04', DATEADD(DAY, -1, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: paediatric homeopathy', 1, N'Open',   NULL),
        (N'S2 Guest 05', DATEADD(DAY, -2, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: booking not confirmed', 1, N'New',    NULL),
        (N'S2 Guest 06', DATEADD(DAY, -2, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: privacy question', 1, N'New',    NULL),
        (N'S2 Guest 07', DATEADD(DAY, -3, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: want in-clinic slot', 1, N'Open',   @DoctorUserId),
        (N'S2 Guest 08', DATEADD(DAY, -3, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: closed after callback', 0, N'Closed', @DoctorUserId),
        (N'S2 Guest 09', DATEADD(DAY, -4, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: medicine order enquiry', 1, N'New',    NULL),
        (N'S2 Guest 10', DATEADD(DAY, -4, @SeedDate), N'tufanpowar001@gmail.com', N'7768046064', N'S2-TESTED: doctor registration help', 1, N'New',    NULL);
    PRINT 'INSERTED 10 EnquiryDetails S2-TESTED';
END
ELSE
BEGIN
    UPDATE dbo.EnquiryDetails
    SET MobileNo = N'7768046064',
        EmailId = N'tufanpowar001@gmail.com'
    WHERE EnquiryName LIKE N'S2 Guest %'
      AND (
            ISNULL(MobileNo, N'') <> N'7768046064'
            OR ISNULL(EmailId, N'') <> N'tufanpowar001@gmail.com'
          );
    PRINT 'SKIP EnquiryDetails S2-TESTED already present (mobile aligned)';
END

-- ---------------------------------------------------------------------------
-- 10 CogRun logs
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.CogRun WHERE InputJson LIKE N'%"marker":"S2-TESTED"%')
BEGIN
    DECLARE @i INT = 1;
    WHILE @i <= 10
    BEGIN
        INSERT INTO dbo.CogRun (DoctorId, PatientId, InputJson, OutputJson, CreatedAt)
        VALUES (
            @DoctorId,
            @PatientId,
            N'{"marker":"S2-TESTED","n":' + CONVERT(NVARCHAR(10), @i) + N',"patientId":' + CONVERT(NVARCHAR(10), @PatientId)
                + N',"rubrics":[{"subSectionId":151632,"intensity":3},{"subSectionId":151643,"intensity":2}]}',
            N'[{"remedyId":2,"remedyName":"S2-TESTED sample","score":' + CONVERT(NVARCHAR(10), 20 - @i)
                + N',"contributingSubSectionIds":[151632,151643],"reason":"Weighted by intensity x grade (S2-TESTED row '
                + CONVERT(NVARCHAR(10), @i) + N')."}]',
            DATEADD(MINUTE, @i, CAST(@SeedDate AS DATETIME))
        );
        SET @i += 1;
    END
    PRINT 'INSERTED 10 CogRun S2-TESTED';
END
ELSE
    PRINT 'SKIP CogRun S2-TESTED already present';

-- ---------------------------------------------------------------------------
-- 10 PatientAppointment public-booking sample rows
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.PatientAppointment WHERE BookingToken LIKE N'S2TESTED%')
BEGIN
    INSERT INTO dbo.PatientAppointment (
        PatientId, AppointmentDate, AppointmentTime, Status, DeleteStatus, UserId, DoctorId,
        BookingToken, VisitType, ConsultMode, PaymentStatus, IsTele, HoldExpiresAt, ConsentPolicyVersion
    )
    VALUES
        (@PatientId, DATEADD(DAY, 0, @SeedDate), CAST('10:00' AS TIME), N'WAITING',     0, @DoctorUserId, @DoctorId, N'S2TESTED01', N'InClinic', N'InClinic', N'PAID',    0, NULL, N'2026.09'),
        (@PatientId, DATEADD(DAY, 0, @SeedDate), CAST('10:15' AS TIME), N'WAITING',     0, @DoctorUserId, @DoctorId, N'S2TESTED02', N'InClinic', N'InClinic', N'PENDING', 0, DATEADD(MINUTE, 15, GETUTCDATE()), N'2026.09'),
        (@PatientId, DATEADD(DAY, 1, @SeedDate), CAST('11:00' AS TIME), N'E-CONSULT',   0, @DoctorUserId, @DoctorId, N'S2TESTED03', N'Tele',     N'Tele',     N'PAID',    1, NULL, N'2026.09'),
        (@PatientId, DATEADD(DAY, 1, @SeedDate), CAST('11:15' AS TIME), N'NOT ARRIVED', 0, @DoctorUserId, @DoctorId, N'S2TESTED04', N'Tele',     N'Tele',     N'PENDING', 1, DATEADD(MINUTE, 15, GETUTCDATE()), N'2026.09'),
        (@PatientId, DATEADD(DAY, 2, @SeedDate), CAST('12:00' AS TIME), N'WALK-IN',     0, @DoctorUserId, @DoctorId, N'S2TESTED05', N'InClinic', N'InClinic', N'UNPAID',  0, NULL, N'2026.09'),
        (@PatientId, DATEADD(DAY, 2, @SeedDate), CAST('12:30' AS TIME), N'WAITING',     0, @DoctorUserId, @DoctorId, N'S2TESTED06', N'InClinic', N'InClinic', N'PAID',    0, NULL, N'2026.09'),
        (@PatientId, DATEADD(DAY, 3, @SeedDate), CAST('09:00' AS TIME), N'REMAINING',   0, @DoctorUserId, @DoctorId, N'S2TESTED07', N'InClinic', N'InClinic', N'PENDING', 0, DATEADD(MINUTE, 15, GETUTCDATE()), N'2026.09'),
        (@PatientId, DATEADD(DAY, 3, @SeedDate), CAST('09:30' AS TIME), N'E-CONSULT',   0, @DoctorUserId, @DoctorId, N'S2TESTED08', N'Tele',     N'Tele',     N'UNPAID',  1, NULL, N'2026.09'),
        (@PatientId, DATEADD(DAY, 4, @SeedDate), CAST('16:00' AS TIME), N'COMPLETED',   0, @DoctorUserId, @DoctorId, N'S2TESTED09', N'InClinic', N'InClinic', N'PAID',    0, NULL, N'2026.09'),
        (@PatientId, DATEADD(DAY, 4, @SeedDate), CAST('16:30' AS TIME), N'WAITING',     0, @DoctorUserId, @DoctorId, N'S2TESTED10', N'InClinic', N'InClinic', N'PENDING', 0, DATEADD(MINUTE, 15, GETUTCDATE()), N'2026.09');
    PRINT 'INSERTED 10 PatientAppointment S2TESTED01-10';
END
ELSE
    PRINT 'SKIP PatientAppointment S2TESTED already present';

-- ---------------------------------------------------------------------------
-- Daily schedule so Public Slots is not empty on seed date + today
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DoctorDailySchedule', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.DoctorDailySchedule WHERE DoctorId = @DoctorId AND ScheduleDate = @SeedDate)
        INSERT INTO dbo.DoctorDailySchedule (DoctorId, ScheduleDate, SlotIntervalMinutes, WorkStartTime, WorkEndTime, CreatedByUserId, CreatedAt)
        VALUES (@DoctorId, @SeedDate, 15, CAST('10:00' AS TIME), CAST('18:00' AS TIME), @DoctorUserId, GETUTCDATE());

    IF NOT EXISTS (SELECT 1 FROM dbo.DoctorDailySchedule WHERE DoctorId = @DoctorId AND ScheduleDate = CAST(GETDATE() AS DATE))
        INSERT INTO dbo.DoctorDailySchedule (DoctorId, ScheduleDate, SlotIntervalMinutes, WorkStartTime, WorkEndTime, CreatedByUserId, CreatedAt)
        VALUES (@DoctorId, CAST(GETDATE() AS DATE), 15, CAST('10:00' AS TIME), CAST('18:00' AS TIME), @DoctorUserId, GETUTCDATE());

    PRINT 'DoctorDailySchedule sample dates ready';
END

-- ---------------------------------------------------------------------------
-- 3D hotspot curated SubSectionId (exact name match was 0)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ThreeDBodyPartSectionHotspot', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ThreeDBodyPartSectionHotspot', N'SubSectionId') IS NOT NULL
BEGIN
    UPDATE h SET h.SubSectionId = s.SubSectionID
    FROM dbo.ThreeDBodyPartSectionHotspot h
    CROSS APPLY (
        SELECT TOP 1 sm.SubSectionID
        FROM dbo.SubSectionMaster sm
        WHERE ISNULL(sm.DeleteStatus, 0) = 0
          AND (
                (h.HotspotName = N'Cervical region' AND sm.SubSectionName = N'BACK-ABSCESS-Cervical region')
             OR (h.HotspotName = N'Dorsal region' AND sm.SubSectionName = N'BACK-ADHERENT-Dorsal region')
             OR (h.HotspotName = N'Lumbar region' AND sm.SubSectionName = N'BACK-NIGHT-Lumbar region')
             OR (h.HotspotName = N'Coccygeal region' AND sm.SubSectionName = N'BACK-ABSCESS-Coccyx, just below')
             OR (h.HotspotName = N'Cervical region-Spine' AND sm.SubSectionName = N'BACK-ABSCESS-Spine')
          )
        ORDER BY sm.SubSectionID
    ) s
    WHERE ISNULL(h.DeleteStatus, 0) = 0
      AND h.SubSectionId IS NULL;

    DECLARE @HotCnt INT = (
        SELECT COUNT(*) FROM dbo.ThreeDBodyPartSectionHotspot
        WHERE SubSectionId IS NOT NULL AND ISNULL(DeleteStatus, 0) = 0
    );
    PRINT CONCAT('Hotspot SubSectionId mapped = ', @HotCnt);
END

-- ---------------------------------------------------------------------------
-- One verified PatientAuth OTP (code 123456 hashed) for booking API tests
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.OtpChallenge WHERE Action = N'PatientAuth' AND EntityId = N'7768046064' AND DestinationMasked LIKE N'%S2%')
BEGIN
    INSERT INTO dbo.OtpChallenge (
        Action, EntityType, EntityId, DestinationMasked, OtpHash,
        ExpiresAt, AttemptCount, CreatedAt, VerifiedAt
    )
    VALUES (
        N'PatientAuth', N'Mobile', N'7768046064', N'S2******6064',
        CONVERT(VARCHAR(64), HASHBYTES(N'SHA2_256', CONVERT(VARBINARY(32), N'123456')), 2),
        DATEADD(DAY, 7, GETUTCDATE()),
        0,
        GETUTCDATE(),
        GETUTCDATE()
    );
    PRINT 'INSERTED OtpChallenge PatientAuth S2-TESTED (dev only)';
END
ELSE
    PRINT 'SKIP OtpChallenge PatientAuth S2-TESTED';

COMMIT TRAN;

DECLARE @Enq INT = (SELECT COUNT(*) FROM dbo.EnquiryDetails WHERE EnquiryName LIKE N'S2 Guest %');
DECLARE @Cog INT = (SELECT COUNT(*) FROM dbo.CogRun WHERE InputJson LIKE N'%"marker":"S2-TESTED"%');
DECLARE @Book INT = (SELECT COUNT(*) FROM dbo.PatientAppointment WHERE BookingToken LIKE N'S2TESTED%');
DECLARE @Kyc INT = (SELECT COUNT(*) FROM dbo.DoctorPayeeKyc WHERE ISNULL(DeleteStatus, 0) = 0);
DECLARE @Vis INT = (SELECT COUNT(*) FROM dbo.Doctor WHERE ISNULL(DeleteStatus, 0) = 0 AND DirectoryVisible = 1 AND VerificationStatus = N'Verified');
DECLARE @Map INT = (SELECT COUNT(*) FROM dbo.ThreeDBodyPartSectionHotspot WHERE SubSectionId IS NOT NULL AND ISNULL(DeleteStatus, 0) = 0);
PRINT '----------------------------------------';
PRINT CONCAT('Enquiry S2-TESTED = ', @Enq);
PRINT CONCAT('CogRun S2-TESTED  = ', @Cog);
PRINT CONCAT('Bookings S2TESTED = ', @Book);
PRINT CONCAT('DoctorPayeeKyc    = ', @Kyc);
PRINT CONCAT('VisibleVerified   = ', @Vis);
PRINT CONCAT('Hotspot mapped    = ', @Map);
PRINT '07_TEST_Sample_Data_Insert.sql complete';
GO

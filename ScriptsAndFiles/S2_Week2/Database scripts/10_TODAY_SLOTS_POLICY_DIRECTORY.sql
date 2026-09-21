/*
================================================================================
Author       : Tufan Powar
Created      : 20-09-2026
Script       : 10_TODAY_SLOTS_POLICY_DIRECTORY.sql
Purpose      : Dev-only static rows so public Find-Doctor slots work today/tomorrow
               for every Verified + DirectoryVisible doctor on HomeoCentrum_Dev.
Use          : Run after 01–09 on HomeoCentrum_Dev only. Do not run on production.
Idempotent   : Yes.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @Tomorrow DATE = DATEADD(DAY, 1, @Today);

IF OBJECT_ID(N'dbo.Doctor', N'U') IS NULL
   OR OBJECT_ID(N'dbo.DoctorDailySchedule', N'U') IS NULL
BEGIN
    RAISERROR('Need Doctor + DoctorDailySchedule. Run scripts 01–07 first.', 16, 1);
    RETURN;
END

BEGIN TRAN;

-- Today's and tomorrow's hours for every verified directory doctor (Public Slots).
INSERT INTO dbo.DoctorDailySchedule (
    DoctorId, ScheduleDate, SlotIntervalMinutes, WorkStartTime, WorkEndTime, CreatedByUserId, CreatedAt
)
SELECT
    d.DoctorId,
    v.ScheduleDate,
    15,
    CAST('10:00' AS TIME),
    CAST('18:00' AS TIME),
    ISNULL(d.UserId, 0),
    GETUTCDATE()
FROM dbo.Doctor d
CROSS JOIN (VALUES (@Today), (@Tomorrow)) v(ScheduleDate)
WHERE ISNULL(d.DeleteStatus, 0) = 0
  AND d.DirectoryVisible = 1
  AND d.VerificationStatus = N'Verified'
  AND NOT EXISTS (
        SELECT 1
        FROM dbo.DoctorDailySchedule s
        WHERE s.DoctorId = d.DoctorId
          AND s.ScheduleDate = v.ScheduleDate
  );

PRINT CONCAT('DoctorDailySchedule upsert for ', CONVERT(CHAR(10), @Today, 23), ' and ', CONVERT(CHAR(10), @Tomorrow, 23));

-- Booking policy row used by public booking consent.
IF OBJECT_ID(N'dbo.PolicyVersion', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.PolicyVersion
        WHERE PolicyType = N'Booking' AND Version = N'2026.09'
   )
BEGIN
    INSERT INTO dbo.PolicyVersion (PolicyType, Version, Title, BodyHtml, IsCurrent)
    VALUES (
        N'Booking',
        N'2026.09',
        N'Booking consent',
        N'<p>Public booking hold is PaymentStatus=PENDING until clinic or Razorpay payment on classic api.</p>',
        1
    );
    PRINT 'INSERTED PolicyVersion Booking 2026.09';
END
ELSE
    PRINT 'SKIP PolicyVersion Booking 2026.09';

COMMIT TRAN;

SELECT
    (SELECT COUNT(*) FROM dbo.Doctor WHERE ISNULL(DeleteStatus, 0) = 0 AND DirectoryVisible = 1 AND VerificationStatus = N'Verified') AS VerifiedDirectoryDoctors,
    (SELECT COUNT(*) FROM dbo.DoctorDailySchedule WHERE ScheduleDate = @Today) AS SchedulesToday,
    (SELECT COUNT(*) FROM dbo.DoctorDailySchedule WHERE ScheduleDate = @Tomorrow) AS SchedulesTomorrow,
    (SELECT COUNT(*) FROM dbo.PolicyVersion WHERE IsCurrent = 1) AS CurrentPolicies;

PRINT '10_TODAY_SLOTS_POLICY_DIRECTORY.sql complete';
GO

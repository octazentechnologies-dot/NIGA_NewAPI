/*
================================================================================
Author       : Tufan Powar
Created      : 22-09-2026
Script       : 03_DEV_Seed_S3_Static.sql
Purpose      : Static Dev rows for Week 3 so patient, doctor mobile, and clinic
               calls return data. Does not rewrite old VisitType. No Razorpay.
Use          : HomeoCentrum_Dev only. Run after 01 and 02.
Marker       : BookingToken S3STATIC01, waitlist mobile 9000001444, ticket S3-STATIC.
Idempotent   : Yes.
Static users : Tufan_Doctor 1010 / UserId 10032
               Tufan_Patient PatientId 3046 / UserId 10033
               Tufan_Admin UserId 10030
               Password 123456 (seeded by Week 2 script 15)
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.HelpArticle', N'U') IS NULL
   OR OBJECT_ID(N'dbo.BookingWaitlist', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
BEGIN
    RAISERROR('Run 01_S3_Week3_Schema.sql first.', 16, 1);
    RETURN;
END

DECLARE @DoctorId INT = 1010;
DECLARE @DoctorUserId BIGINT = 10032;
DECLARE @PatientId INT = 3046;
DECLARE @PatientUserId BIGINT = 10033;
DECLARE @AdminUserId BIGINT = 10030;

IF NOT EXISTS (SELECT 1 FROM dbo.Doctor WHERE DoctorId = @DoctorId AND UserId = @DoctorUserId)
   OR NOT EXISTS (SELECT 1 FROM dbo.Patient WHERE PatientID = @PatientId)
   OR NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserId = @PatientUserId AND UserName = N'Tufan_Patient')
BEGIN
    RAISERROR('Static Tufan_Doctor 1010 or Tufan_Patient 3046 is missing. Run Week 2 scripts 15 and 24 first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Slug = N'booking-a-visit')
    INSERT INTO dbo.HelpArticle (Title, Slug, Body, IsPublished)
    VALUES (N'Booking a visit', N'booking-a-visit', N'Choose a doctor, an open slot, and in-clinic or tele. A full day can take a waitlist join. Joining does not reserve the slot.', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Slug = N'draft-hidden-article')
    INSERT INTO dbo.HelpArticle (Title, Slug, Body, IsPublished)
    VALUES (N'Draft hidden article', N'draft-hidden-article', N'This article stays hidden until it is published.', 0);

IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Slug = N's3-static-cancel')
    INSERT INTO dbo.HelpArticle (Title, Slug, Body, IsPublished)
    VALUES (N'Cancel a visit', N's3-static-cancel', N'Cancel before the visit starts. Reason Other needs a short note. A cancelled visit does not hold the slot.', 1);

IF NOT EXISTS (
    SELECT 1 FROM dbo.BookingWaitlist
    WHERE DoctorId = @DoctorId AND ContactMobile = N'9000001444' AND RequestedDate = '2026-12-20'
)
    INSERT INTO dbo.BookingWaitlist (DoctorId, PatientId, RequestedDate, ConsultMode, ContactName, ContactMobile, Status)
    VALUES (@DoctorId, @PatientId, '2026-12-20', N'InClinic', N'S3-STATIC Sanjay Patil', N'9000001444', N'JOINED');

IF NOT EXISTS (SELECT 1 FROM dbo.DoctorDailySchedule WHERE DoctorId = @DoctorId AND ScheduleDate = '2026-12-20')
    INSERT INTO dbo.DoctorDailySchedule
        (DoctorId, ScheduleDate, SlotIntervalMinutes, WorkStartTime, WorkEndTime, CreatedByUserId, CreatedAt, BreakStartTime, BreakEndTime)
    VALUES
        (@DoctorId, '2026-12-20', 15, '09:00', '12:00', @DoctorUserId, GETDATE(), '10:30', '10:45');

DECLARE @AppId INT;
SELECT @AppId = PatientAppId
FROM dbo.PatientAppointment
WHERE BookingToken = N'S3STATIC01' AND ISNULL(DeleteStatus, 0) = 0;

IF @AppId IS NULL
BEGIN
    INSERT INTO dbo.PatientAppointment
        (PatientId, AppointmentTime, Status, DeleteStatus, UserId, DoctorId, AppointmentDate,
         BookingToken, VisitType, ConsultMode, PaymentStatus, IsTele, PayAtClinicAllowed, QueuePosition, BookingChannel)
    VALUES
        (@PatientId, '11:00', N'WAITING', 0, @DoctorUserId, @DoctorId, '2026-12-21',
         N'S3STATIC01', N'InClinic', N'InClinic', N'PENDING', 0, 1, 1, N'Staff');
    SET @AppId = SCOPE_IDENTITY();
END

IF OBJECT_ID(N'dbo.TeleAvailability', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.TeleAvailability WHERE DoctorId = @DoctorId)
        INSERT INTO dbo.TeleAvailability (DoctorId, IsOnline, LastHeartbeat) VALUES (@DoctorId, 1, GETDATE());
    ELSE
        UPDATE dbo.TeleAvailability SET IsOnline = 1, LastHeartbeat = GETDATE() WHERE DoctorId = @DoctorId AND IsOnline = 0;
END

IF OBJECT_ID(N'dbo.SupportTicket', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SupportTicket WHERE Subject = N'S3-STATIC booking help' AND ReporterUserId = @PatientUserId)
BEGIN
    INSERT INTO dbo.SupportTicket
        (ReporterUserId, ReporterRole, Category, Subject, Body, Status, Priority, CreatedAt)
    VALUES
        (@PatientUserId, N'Patient', N'Booking', N'S3-STATIC booking help', N'Static sample. The patient cannot see the 21 Dec visit.', N'OPEN', N'NORMAL', GETDATE());
    DECLARE @TicketId INT = SCOPE_IDENTITY();
    INSERT INTO dbo.SupportTicketMessage (SupportTicketId, AuthorUserId, AuthorRole, Body)
    VALUES (@TicketId, @AdminUserId, N'Admin', N'S3-STATIC reply. Check appointment S3STATIC01.');
END

IF OBJECT_ID(N'dbo.InstantConsultRequest', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.InstantConsultRequest WHERE ContactMobile = N'9000001555')
    INSERT INTO dbo.InstantConsultRequest (PatientId, ContactName, ContactMobile, Status, QueuePosition)
    VALUES (@PatientId, N'S3-STATIC Instant', N'9000001555', N'OPEN', 1);

PRINT 'S3 static seed ready. Appointment token S3STATIC01, patient 3046, doctor 1010.';
GO

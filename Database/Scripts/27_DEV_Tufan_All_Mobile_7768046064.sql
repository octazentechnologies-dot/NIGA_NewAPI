/*
================================================================================
Author       : Tufan Powar
Created      : 23-09-2026
Script       : 27_DEV_Tufan_All_Mobile_7768046064.sql
Purpose      : Every seeded / Dev-test login and related Patient / Doctor /
               reception / enquiry / waitlist / instant / OTP row uses mobile
               7768046064. Dummy 900000* / 910000* / 900001* / 920000* numbers
               are replaced. Real clinic numbers (e.g. TestStaff) stay.
Use          : HomeoCentrum_Dev only. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

DECLARE @M NVARCHAR(20) = N'7768046064';

BEGIN TRAN;

UPDATE dbo.UserMaster
SET MobileNo = @M
WHERE ISNULL(DeleteStatus, 0) = 0
  AND (
        UserName LIKE N'Tufan[_]%'
        OR UserName LIKE N'tufan%'
        OR UserName LIKE N's1.%'
        OR UserName LIKE N's2.%'
        OR EmailId LIKE N'tufanpowar%'
        OR EmailId LIKE N'%homeocentrum.dev'
        OR MobileNo LIKE N'900000%'
        OR MobileNo LIKE N'910000%'
        OR MobileNo LIKE N'900001%'
        OR MobileNo LIKE N'920000%'
      )
  AND ISNULL(MobileNo, N'') <> @M;
PRINT CONCAT('UPDATED UserMaster mobiles=', @@ROWCOUNT);

IF COL_LENGTH(N'dbo.Doctor', N'MobileNo') IS NOT NULL
BEGIN
    UPDATE d
    SET d.MobileNo = @M
    FROM dbo.Doctor d
    LEFT JOIN dbo.UserMaster u ON u.UserId = d.UserId
    WHERE ISNULL(d.DeleteStatus, 0) = 0
      AND (
            d.DoctorID = 1010
            OR ISNULL(u.UserName, N'') LIKE N'Tufan[_]%'
            OR ISNULL(u.UserName, N'') LIKE N'tufan%'
            OR d.MobileNo LIKE N'900000%'
            OR d.MobileNo LIKE N'910000%'
            OR d.MobileNo LIKE N'900001%'
            OR d.MobileNo LIKE N'920000%'
          )
      AND ISNULL(d.MobileNo, N'') <> @M;
    PRINT CONCAT('UPDATED Doctor mobiles=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.DoctorReceptionStaff
    SET ContactNumber = @M
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND (
            UserID LIKE N'Tufan%'
            OR UserID LIKE N's2.%'
            OR FullName LIKE N'Tufan%'
            OR EmailId LIKE N'tufanpowar%'
            OR EmailId LIKE N'%homeocentrum.dev'
            OR ContactNumber LIKE N'900000%'
            OR ContactNumber LIKE N'910000%'
            OR ContactNumber LIKE N'900001%'
            OR ContactNumber LIKE N'920000%'
          )
      AND ISNULL(ContactNumber, N'') <> @M;
    PRINT CONCAT('UPDATED DoctorReceptionStaff contacts=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.Patient', N'U') IS NOT NULL
BEGIN
    UPDATE p
    SET p.MobileNo = @M,
        p.PhoneNo = CASE
            WHEN p.PhoneNo LIKE N'900000%'
              OR p.PhoneNo LIKE N'910000%'
              OR p.PhoneNo LIKE N'900001%'
              OR p.PhoneNo LIKE N'920000%'
            THEN @M
            ELSE p.PhoneNo
        END
    FROM dbo.Patient p
    WHERE ISNULL(p.DeleteStatus, 0) = 0
      AND (
            p.PatientName LIKE N'Tufan%'
            OR p.PatientName LIKE N'Anita%'
            OR p.PatientName LIKE N'Audit Spouse%'
            OR p.PatientName LIKE N'%S1-TESTED%'
            OR p.PatientName LIKE N'S1 Caregiver%'
            OR p.PatientName LIKE N'Clinic Demo%'
            OR p.PatientName LIKE N'S2 Demo%'
            OR p.PatientName LIKE N'SMTP Family%'
            OR p.PatientName LIKE N'%Sample%'
            OR p.PatientName LIKE N'CON Demo%'
            OR p.PatientName LIKE N'S1 Live%'
            OR p.PatientName LIKE N'S2 Live%'
            OR p.PatientName LIKE N'Public Second%'
            OR p.PatientName LIKE N'Dev Spouse%'
            OR p.PatientName LIKE N'LineByLine%'
            OR ISNULL(p.EnteredBy, N'') LIKE N'TUFAN%'
            OR ISNULL(p.EnteredBy, N'') LIKE N'S1-TESTED%'
            OR ISNULL(p.EnteredBy, N'') LIKE N'TUFAN-DOC%'
            OR p.MobileNo LIKE N'900000%'
            OR p.MobileNo LIKE N'910000%'
            OR p.MobileNo LIKE N'900001%'
            OR p.MobileNo LIKE N'920000%'
            OR p.PhoneNo LIKE N'900000%'
            OR p.PhoneNo LIKE N'910000%'
            OR p.PhoneNo LIKE N'900001%'
            OR p.PhoneNo LIKE N'920000%'
            OR EXISTS (
                SELECT 1
                FROM dbo.PatientUserMap m
                INNER JOIN dbo.UserMaster u ON u.UserId = m.UserId
                WHERE m.PatientId = p.PatientId
                  AND ISNULL(m.DeleteStatus, 0) = 0
                  AND (
                        u.UserName LIKE N'Tufan[_]%'
                        OR u.UserName LIKE N'tufan%'
                        OR u.UserName LIKE N's1.%'
                        OR u.UserName LIKE N's2.%'
                      )
            )
          )
      AND (
            ISNULL(p.MobileNo, N'') <> @M
            OR p.PhoneNo LIKE N'900000%'
            OR p.PhoneNo LIKE N'910000%'
            OR p.PhoneNo LIKE N'900001%'
            OR p.PhoneNo LIKE N'920000%'
          );
    PRINT CONCAT('UPDATED Patient mobiles=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.EnquiryDetails', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.EnquiryDetails
    SET MobileNo = @M
    WHERE (
            EmailId LIKE N'%homeocentrum.dev'
            OR EmailId LIKE N's2.%'
            OR EnquiryName LIKE N'S2 Guest%'
            OR EnquiryName LIKE N'S2 Live%'
            OR EnquiryName LIKE N'Dev Enquiry%'
            OR EnquiryName LIKE N'QA %'
            OR EnquiryName LIKE N'LineByLine%'
            OR MobileNo LIKE N'900000%'
            OR MobileNo LIKE N'910000%'
            OR MobileNo LIKE N'900001%'
            OR MobileNo LIKE N'920000%'
          )
      AND ISNULL(MobileNo, N'') <> @M;
    PRINT CONCAT('UPDATED EnquiryDetails mobiles=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.BookingWaitlist', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.BookingWaitlist
    SET ContactMobile = @M
    WHERE (
            ContactName LIKE N'%Wait%'
            OR ContactName LIKE N'S3-STATIC%'
            OR ContactName LIKE N'Dev Waitlist%'
            OR ContactName LIKE N'Second Pass%'
            OR ContactName LIKE N'No Offer%'
            OR ContactName LIKE N'LineByLine%'
            OR ContactName LIKE N'QA %'
            OR ContactMobile LIKE N'900000%'
            OR ContactMobile LIKE N'910000%'
            OR ContactMobile LIKE N'900001%'
            OR ContactMobile LIKE N'920000%'
          )
      AND ISNULL(ContactMobile, N'') <> @M;
    PRINT CONCAT('UPDATED BookingWaitlist mobiles=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.InstantConsultRequest', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.InstantConsultRequest
    SET ContactMobile = @M
    WHERE (
            ContactName LIKE N'Instant%'
            OR ContactName LIKE N'S3-STATIC%'
            OR ContactName LIKE N'QA %'
            OR ContactName LIKE N'LineByLine%'
            OR ContactMobile LIKE N'900000%'
            OR ContactMobile LIKE N'910000%'
            OR ContactMobile LIKE N'900001%'
            OR ContactMobile LIKE N'920000%'
          )
      AND ISNULL(ContactMobile, N'') <> @M;
    PRINT CONCAT('UPDATED InstantConsultRequest mobiles=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.PatientHealthBasics', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientHealthBasics', N'EmergencyContactMobile') IS NOT NULL
BEGIN
    UPDATE dbo.PatientHealthBasics
    SET EmergencyContactMobile = @M
    WHERE (
            EmergencyContactMobile LIKE N'900000%'
            OR EmergencyContactMobile LIKE N'910000%'
            OR EmergencyContactMobile LIKE N'900001%'
            OR EmergencyContactMobile LIKE N'920000%'
          )
      AND ISNULL(EmergencyContactMobile, N'') <> @M;
    PRINT CONCAT('UPDATED PatientHealthBasics emergency mobiles=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.OtpChallenge', N'EntityId') IS NOT NULL
BEGIN
    UPDATE dbo.OtpChallenge
    SET EntityId = @M
    WHERE EntityId LIKE N'900000%'
       OR EntityId LIKE N'910000%'
       OR EntityId LIKE N'900001%'
       OR EntityId LIKE N'920000%';
    PRINT CONCAT('UPDATED OtpChallenge EntityId=', @@ROWCOUNT);
END

COMMIT TRAN;

PRINT '27_DEV_Tufan_All_Mobile_7768046064.sql completed.';
GO

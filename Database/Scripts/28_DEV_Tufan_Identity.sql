/*
================================================================================
Author       : Tufan Powar
Created      : 23-09-2026
Script       : 28_DEV_Tufan_Identity.sql
Purpose      : Every user we created uses:
                 UserName  Tufan_<role>
                 EmailId   tufanpowar001@gmail.com
                 MobileNo  7768046064
               Seeded Patient / Enquiry / reception contact rows use the same
               email and mobile. Leftover s1.* / s2.* UserName rows keep the
               unique login name (cannot collide with Tufan_Account etc.) but
               EmailId/MobileNo are aligned. New scripts must not insert other
               emails or mobiles.
Use          : HomeoCentrum_Dev only. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

DECLARE @E NVARCHAR(200) = N'tufanpowar001@gmail.com';
DECLARE @M NVARCHAR(20)  = N'7768046064';

BEGIN TRAN;

UPDATE dbo.UserMaster
SET EmailId = @E,
    MobileNo = @M
WHERE ISNULL(DeleteStatus, 0) = 0
  AND (
        UserName LIKE N'Tufan[_]%'
        OR UserName LIKE N'tufan%'
        OR UserName LIKE N's1.%'
        OR UserName LIKE N's2.%'
        OR EmailId LIKE N'tufanpowar%'
        OR EmailId LIKE N'%homeocentrum.dev'
        OR EmailId LIKE N's1.%'
        OR EmailId LIKE N's2.%'
      )
  AND (
        ISNULL(EmailId, N'') <> @E
        OR ISNULL(MobileNo, N'') <> @M
      );
PRINT CONCAT('UPDATED UserMaster email+mobile=', @@ROWCOUNT);

IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.DoctorReceptionStaff
    SET EmailId = @E,
        ContactNumber = @M
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND (
            UserID LIKE N'Tufan%'
            OR UserID LIKE N's2.%'
            OR EmailId LIKE N'%homeocentrum.dev'
            OR EmailId LIKE N'tufanpowar%'
          )
      AND (
            ISNULL(EmailId, N'') <> @E
            OR ISNULL(ContactNumber, N'') <> @M
          );
    PRINT CONCAT('UPDATED DoctorReceptionStaff email+mobile=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.Patient', N'U') IS NOT NULL
BEGIN
    UPDATE p
    SET p.Email = @E,
        p.MobileNo = @M
    FROM dbo.Patient p
    WHERE ISNULL(p.DeleteStatus, 0) = 0
      AND (
            p.PatientName LIKE N'Tufan%'
            OR p.PatientName LIKE N'Anita%'
            OR p.PatientName LIKE N'Clinic Demo%'
            OR p.PatientName LIKE N'%Sample%'
            OR p.PatientName LIKE N'%S1-TESTED%'
            OR p.PatientName LIKE N'S1 Caregiver%'
            OR p.PatientName LIKE N'S1 Live%'
            OR p.PatientName LIKE N'S2 Live%'
            OR p.PatientName LIKE N'S2 Demo%'
            OR p.PatientName LIKE N'CON Demo%'
            OR p.PatientName LIKE N'Audit Spouse%'
            OR p.PatientName LIKE N'Dev Spouse%'
            OR p.PatientName LIKE N'LineByLine%'
            OR p.PatientName LIKE N'Public Second%'
            OR p.PatientName LIKE N'SMTP Family%'
            OR p.Email LIKE N'%homeocentrum.dev'
            OR p.Email LIKE N's1.%'
            OR p.Email LIKE N's2.%'
            OR ISNULL(p.EnteredBy, N'') LIKE N'TUFAN%'
            OR ISNULL(p.EnteredBy, N'') LIKE N'S1-TESTED%'
            OR ISNULL(p.EnteredBy, N'') LIKE N'S1-SAMPLE%'
          )
      AND (
            ISNULL(p.Email, N'') <> @E
            OR ISNULL(p.MobileNo, N'') <> @M
          );
    PRINT CONCAT('UPDATED Patient email+mobile=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.EnquiryDetails', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.EnquiryDetails
    SET EmailId = @E,
        MobileNo = @M
    WHERE (
            EmailId LIKE N'%homeocentrum.dev'
            OR EmailId LIKE N's2.%'
            OR EmailId LIKE N's1.%'
            OR EnquiryName LIKE N'S2 Guest%'
            OR EnquiryName LIKE N'S2 Live%'
            OR EnquiryName LIKE N'Dev Enquiry%'
            OR EnquiryName LIKE N'QA %'
            OR EnquiryName LIKE N'LineByLine%'
          )
      AND (
            ISNULL(EmailId, N'') <> @E
            OR ISNULL(MobileNo, N'') <> @M
          );
    PRINT CONCAT('UPDATED EnquiryDetails email+mobile=', @@ROWCOUNT);
END

COMMIT TRAN;

PRINT '28_DEV_Tufan_Identity.sql completed.';
PRINT 'Login with Tufan_Admin / Tufan_Doctor / Tufan_Reception / Tufan_Account /';
PRINT 'Tufan_Pharmacy / Tufan_Patient / Tufan_Caregiver / Tufan_NoMenu  password 123456.';
PRINT 'Email tufanpowar001@gmail.com  mobile 7768046064.';
GO

/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 17_DEV_Tufan_Contact_Email_Mobile.sql
Purpose      : Put tufanpowar001@gmail.com / 7768046064 on Tufan_Patient (and
               Tufan_Reception staff row). Other Tufan_* UserMaster rows keep
               unique emails/mobiles because LoginWithOtp and ForgotPassword
               resolve the first matching UserMaster row.
Use          : HomeoCentrum_Dev only. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

BEGIN TRAN;

-- OTP login matches the first UserMaster.MobileNo. Free 7768046064 from the
-- older tufanpowar001 seed so Tufan_Patient can own the team contact number.
IF EXISTS (
    SELECT 1 FROM dbo.UserMaster
    WHERE UserName = N'tufanpowar001@gmail.com'
      AND ISNULL(DeleteStatus, 0) = 0
      AND MobileNo = N'7768046064'
)
BEGIN
    UPDATE dbo.UserMaster
    SET MobileNo = N'9000000101'
    WHERE UserName = N'tufanpowar001@gmail.com' AND ISNULL(DeleteStatus, 0) = 0;
    PRINT 'UPDATED tufanpowar001@gmail.com MobileNo -> 9000000101 (OTP uniqueness)';
END

IF EXISTS (
    SELECT 1 FROM dbo.UserMaster
    WHERE UserName = N'tufanpowar001@gmail.com'
      AND ISNULL(DeleteStatus, 0) = 0
      AND EmailId = N'tufanpowar001@gmail.com'
)
BEGIN
    UPDATE dbo.UserMaster
    SET EmailId = N'tufan.seed001@homeocentrum.dev'
    WHERE UserName = N'tufanpowar001@gmail.com' AND ISNULL(DeleteStatus, 0) = 0;
    PRINT 'UPDATED tufanpowar001@gmail.com EmailId -> tufan.seed001@homeocentrum.dev (OTP uniqueness)';
END

IF OBJECT_ID(N'dbo.Patient', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.Patient WHERE PatientId = 3033 AND MobileNo = N'7768046064')
BEGIN
    UPDATE dbo.Patient
    SET MobileNo = N'9000000101'
    WHERE PatientId = 3033;
    PRINT 'UPDATED Patient 3033 MobileNo -> 9000000101';
END

UPDATE dbo.UserMaster
SET EmailId = N'tufanpowar001@gmail.com',
    MobileNo = N'7768046064'
WHERE UserName = N'Tufan_Patient' AND ISNULL(DeleteStatus, 0) = 0;
PRINT CONCAT('UPDATED Tufan_Patient UserId=', @@ROWCOUNT);

DECLARE @PatUserId BIGINT = (
    SELECT TOP 1 UserId FROM dbo.UserMaster
    WHERE UserName = N'Tufan_Patient' AND ISNULL(DeleteStatus, 0) = 0
);

IF @PatUserId IS NOT NULL AND OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NOT NULL
BEGIN
    UPDATE p
    SET p.Email = N'tufanpowar001@gmail.com',
        p.MobileNo = N'7768046064'
    FROM dbo.Patient p
    INNER JOIN dbo.PatientUserMap m ON m.PatientId = p.PatientId
    WHERE m.UserId = @PatUserId
      AND ISNULL(m.DeleteStatus, 0) = 0
      AND ISNULL(m.IsPrimary, 1) = 1;
    PRINT CONCAT('UPDATED Tufan_Patient clinical Patient rows=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.DoctorReceptionStaff
    SET EmailId = N'tufanpowar001@gmail.com',
        ContactNumber = N'7768046064'
    WHERE UserID = N'Tufan_Reception' AND ISNULL(DeleteStatus, 0) = 0;
    PRINT CONCAT('UPDATED Tufan_Reception staff contact rows=', @@ROWCOUNT);
END

COMMIT TRAN;

PRINT '17_DEV_Tufan_Contact_Email_Mobile.sql completed.';
PRINT 'Shared contact on Tufan_Patient (+ reception staff Email/Contact).';
PRINT 'Other Tufan_* UserMaster emails/mobiles stay unique.';
GO

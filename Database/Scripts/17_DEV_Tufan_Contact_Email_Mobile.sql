/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 17_DEV_Tufan_Contact_Email_Mobile.sql
Purpose      : Put tufanpowar001@gmail.com / 7768046064 on every Tufan_* login
               (UserMaster, mapped Patient, Tufan_Reception staff). Do not write
               tufan.seed001@homeocentrum.dev.
Use          : HomeoCentrum_Dev only. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

BEGIN TRAN;

-- All Tufan test logins share 7768046064 (same as the team contact number).
UPDATE dbo.UserMaster
SET EmailId = N'tufanpowar001@gmail.com',
    MobileNo = N'7768046064'
WHERE ISNULL(DeleteStatus, 0) = 0
  AND (
        UserName IN (N'Tufan_Admin', N'Tufan_Doctor', N'Tufan_Account', N'Tufan_Pharmacy', N'Tufan_Caregiver', N'Tufan_Patient', N'Tufan_NoMenu', N'tufanpowar001@gmail.com')
        OR EmailId = N'tufan.seed001@homeocentrum.dev'
        OR EmailId LIKE N'tufan.%@homeocentrum.dev'
      )
  AND UserName NOT IN (N'admin', N'NIGA HOMEOPATHY', N'demotestuser', N'NikamGourav', N'riyasawalkar', N'string1', N'Gourav7468', N'testdoctor');
PRINT CONCAT('UPDATED Tufan role EmailId+MobileNo rows=', @@ROWCOUNT);

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
PRINT 'Tufan role users and reception staff use tufanpowar001@gmail.com.';
PRINT 'Listed existing users were not changed.';
GO

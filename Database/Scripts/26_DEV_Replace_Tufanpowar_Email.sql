/*
================================================================================
Author       : Tufan Powar
Created      : 22-09-2026
Script       : 26_DEV_Replace_Tufanpowar_Email.sql
Purpose      : Use tufanpowar001@gmail.com everywhere tufanpowar@gmail.com was.
               Tufan_Patient UserMaster keeps OTP uniqueness on 001 / 7768046064.
               The leftover UserName tufanpowar001@gmail.com row gets a unique
               EmailId so LoginWithOtp is not ambiguous.
Use          : HomeoCentrum_Dev. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

BEGIN TRAN;

-- Seed user whose UserName is tufanpowar001@gmail.com already had EmailId 001.
-- Move that EmailId so Tufan_Patient can own 001 for OTP.
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
    PRINT 'UPDATED seed UserName tufanpowar001@gmail.com EmailId -> tufan.seed001@homeocentrum.dev';
END

UPDATE dbo.UserMaster
SET EmailId = N'tufanpowar001@gmail.com'
WHERE EmailId = N'tufanpowar@gmail.com';
PRINT CONCAT('UPDATED UserMaster EmailId rows=', @@ROWCOUNT);

IF OBJECT_ID(N'dbo.Patient', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.Patient
    SET Email = N'tufanpowar001@gmail.com'
    WHERE Email = N'tufanpowar@gmail.com';
    PRINT CONCAT('UPDATED Patient Email rows=', @@ROWCOUNT);
END

IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.DoctorReceptionStaff
    SET EmailId = N'tufanpowar001@gmail.com'
    WHERE EmailId = N'tufanpowar@gmail.com';
    PRINT CONCAT('UPDATED DoctorReceptionStaff EmailId rows=', @@ROWCOUNT);
END

COMMIT TRAN;

PRINT '26_DEV_Replace_Tufanpowar_Email.sql completed.';
PRINT 'tufanpowar@gmail.com removed. Tufan_Patient EmailId = tufanpowar001@gmail.com.';
GO

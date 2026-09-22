/*
================================================================================
Author       : Tufan Powar
Created      : 22-09-2026
Script       : 26_DEV_Replace_Tufanpowar_Email.sql
Purpose      : Use tufanpowar001@gmail.com everywhere tufanpowar@gmail.com was.
               Tufan_Patient UserMaster keeps OTP uniqueness on 001 / 7768046064.
               Newly created Tufan users keep EmailId tufanpowar001@gmail.com.
               tufan.seed001@homeocentrum.dev is not used.
Use          : HomeoCentrum_Dev. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

BEGIN TRAN;

UPDATE dbo.UserMaster
SET EmailId = N'tufanpowar001@gmail.com'
WHERE ISNULL(DeleteStatus, 0) = 0
  AND EmailId = N'tufan.seed001@homeocentrum.dev';
PRINT CONCAT('REMOVED seed001 EmailId rows=', @@ROWCOUNT);

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

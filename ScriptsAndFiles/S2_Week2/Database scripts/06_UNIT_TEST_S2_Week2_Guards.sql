/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 06_UNIT_TEST_S2_Week2_Guards.sql
Purpose      : Isolated guard tests (create-if-missing, add-column, insert-if-absent)
               plus live HomeoCentrum_Dev assertions for Week 2 schema.
Use          : Run after 01–05.
Idempotent   : Yes. Temp objects are dropped. Live checks are read-only.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @Fail INT = 0;
DECLARE @Pass INT = 0;

IF OBJECT_ID('tempdb..#UtS2') IS NOT NULL DROP TABLE #UtS2;

IF OBJECT_ID('tempdb..#UtS2') IS NULL
BEGIN
    CREATE TABLE #UtS2 (Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(50) NOT NULL);
END

IF OBJECT_ID('tempdb..#UtS2') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: UT-01 create table when missing'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: UT-01 create table when missing'; END

IF NOT EXISTS (SELECT 1 FROM tempdb.sys.columns WHERE object_id = OBJECT_ID(N'tempdb..#UtS2') AND name = N'Code')
    ALTER TABLE #UtS2 ADD Code NVARCHAR(20) NULL;

IF EXISTS (SELECT 1 FROM tempdb.sys.columns WHERE object_id = OBJECT_ID(N'tempdb..#UtS2') AND name = N'Code')
BEGIN SET @Pass += 1; PRINT 'PASS: UT-02 add column when missing'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: UT-02 add column when missing'; END

INSERT INTO #UtS2 (Id, Name, Code) SELECT 1, N'Directory', N'D' WHERE NOT EXISTS (SELECT 1 FROM #UtS2 WHERE Id = 1);
INSERT INTO #UtS2 (Id, Name, Code) SELECT 1, N'Directory', N'D' WHERE NOT EXISTS (SELECT 1 FROM #UtS2 WHERE Id = 1);
IF (SELECT COUNT(*) FROM #UtS2 WHERE Id = 1) = 1
BEGIN SET @Pass += 1; PRINT 'PASS: UT-03 insert-if-absent does not duplicate'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: UT-03 insert-if-absent does not duplicate'; END

IF COL_LENGTH(N'dbo.Doctor', N'DirectoryVisible') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: LIVE Doctor.DirectoryVisible'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: LIVE Doctor.DirectoryVisible'; END

IF OBJECT_ID(N'dbo.DoctorPayeeKyc', N'U') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: LIVE DoctorPayeeKyc'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: LIVE DoctorPayeeKyc'; END

IF COL_LENGTH(N'dbo.PatientAppointment', N'BookingToken') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: LIVE PatientAppointment.BookingToken'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: LIVE PatientAppointment.BookingToken'; END

IF OBJECT_ID(N'dbo.PolicyVersion', N'U') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: LIVE PolicyVersion'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: LIVE PolicyVersion'; END

IF COL_LENGTH(N'dbo.EnquiryDetails', N'TicketStatus') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: LIVE EnquiryDetails.TicketStatus'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: LIVE EnquiryDetails.TicketStatus'; END

IF OBJECT_ID(N'dbo.CogRun', N'U') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: LIVE CogRun'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: LIVE CogRun'; END

IF COL_LENGTH(N'dbo.UserMaster', N'ActivationTokenHash') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: LIVE UserMaster.ActivationTokenHash'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: LIVE UserMaster.ActivationTokenHash'; END

IF COL_LENGTH(N'dbo.ThreeDBodyPartSectionHotspot', N'SubSectionId') IS NOT NULL
BEGIN SET @Pass += 1; PRINT 'PASS: LIVE ThreeDBodyPartSectionHotspot.SubSectionId'; END
ELSE BEGIN SET @Fail += 1; PRINT 'FAIL: LIVE ThreeDBodyPartSectionHotspot.SubSectionId'; END

IF OBJECT_ID('tempdb..#UtS2') IS NOT NULL DROP TABLE #UtS2;

PRINT '----------------------------------------';
PRINT CONCAT('S2 Week 2 guards PASS=', @Pass, ' FAIL=', @Fail);
IF @Fail > 0
    RAISERROR('S2 Week 2 unit/live guards failed.', 16, 1);
GO

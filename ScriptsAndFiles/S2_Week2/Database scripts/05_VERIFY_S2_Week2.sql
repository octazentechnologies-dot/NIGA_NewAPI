/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 05_VERIFY_S2_Week2.sql
Purpose      : Read-only proof that Week 2 schema exists (Doctor directory/KYC,
               booking token, enquiry ticket, CogRun, activation TTL, 3D hotspot).
Use          : Run after 01–04.
Prerequisites: Scripts 01–04.
Idempotent   : Yes (select only).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '--- Doctor profile / directory columns ---';
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'Doctor'
  AND COLUMN_NAME IN (N'ClinicName', N'ConsultFeeInClinic', N'ConsultFeeTele', N'PhotoPath',
                      N'IsOnline', N'DirectoryVisible', N'VerificationStatus', N'PracticeActivated');

PRINT '--- DoctorPayeeKyc ---';
SELECT name FROM sys.tables WHERE name = N'DoctorPayeeKyc';

PRINT '--- Existing doctors directory flags ---';
SELECT COUNT(*) AS VisibleVerified
FROM dbo.Doctor
WHERE ISNULL(DeleteStatus, 0) = 0 AND DirectoryVisible = 1 AND VerificationStatus = N'Verified';

PRINT '--- PatientAppointment booking columns ---';
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'PatientAppointment'
  AND COLUMN_NAME IN (N'BookingToken', N'VisitType', N'ConsultMode', N'PaymentStatus', N'IsTele', N'HoldExpiresAt');

PRINT '--- PolicyVersion ---';
IF OBJECT_ID(N'dbo.PolicyVersion', N'U') IS NULL
    PRINT 'MISSING PolicyVersion';
ELSE
    SELECT PolicyType, Version, IsCurrent FROM dbo.PolicyVersion ORDER BY PolicyType;

PRINT '--- EnquiryDetails TicketStatus/AssignedTo ---';
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'EnquiryDetails' AND COLUMN_NAME IN (N'TicketStatus', N'AssignedTo');

PRINT '--- CogRun / UserMaster activation ---';
SELECT name FROM sys.tables WHERE name IN (N'CogRun');
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'UserMaster' AND COLUMN_NAME IN (N'ActivationTokenHash', N'ActivationExpiresAt');

PRINT '--- 3D hotspot SubSectionId ---';
SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'ThreeDBodyPartSectionHotspot' AND COLUMN_NAME = N'SubSectionId';

PRINT '--- Confirm-no-new-table (CLN clipboard/MM/repertorize/3D masters) ---';
SELECT name
FROM sys.tables
WHERE name IN (N'ClipboardRubrics', N'MateriaMedicaMaster', N'ThreeDBodyPartMeshKeyMaster', N'DoctorDailySchedule', N'DoctorReceptionStaff');

PRINT '05_VERIFY_S2_Week2.sql complete';
GO

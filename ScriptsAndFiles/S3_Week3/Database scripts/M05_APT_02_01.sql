/*
================================================================================
Author       : Tufan Powar
Created      : 23-09-2026
Script       : M05_APT_02_01.sql
Purpose      : APT-02.01 — PatientAppointment.VisitType and ConsultMode.
               Blank VisitType on existing rows becomes First.
               Does not look at earlier appointments to decide FollowUp.
               Does not change ConsultMode and does not overwrite a VisitType
               that is already stored.
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.PatientAppointment exists.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
BEGIN
    RAISERROR('dbo.PatientAppointment is missing. Stop.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.PatientAppointment', N'VisitType') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD VisitType NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'ConsultMode') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD ConsultMode NVARCHAR(50) NULL;
GO

UPDATE dbo.PatientAppointment
SET VisitType = N'First'
WHERE VisitType IS NULL
   OR LTRIM(RTRIM(VisitType)) = N'';
GO

PRINT 'M05_APT_02_01.sql complete';
SELECT
    SUM(CASE WHEN VisitType = N'First' THEN 1 ELSE 0 END) AS VisitTypeFirst,
    SUM(CASE WHEN VisitType IS NULL OR LTRIM(RTRIM(VisitType)) = N'' THEN 1 ELSE 0 END) AS VisitTypeBlank
FROM dbo.PatientAppointment;
GO

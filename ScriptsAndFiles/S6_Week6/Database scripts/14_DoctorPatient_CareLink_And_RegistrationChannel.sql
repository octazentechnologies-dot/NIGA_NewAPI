/*
  DEPRECATED FILENAME — prefer 14_ClinicPatientRoster_And_RegistrationChannel.sql
  (identical content). Clinic patient roster + registration channel.
  1) RegistrationChannel on UserMaster / Patient / Doctor (platform of first signup).
  2) Backfill CaseEntryDetail for appointments that lack a doctor–patient case
     (so mobile bookers appear on doctor All Patients / New Appointment dropdown).
  Not related to product CareLink (doctor-to-doctor second opinion).

  Run with: sqlcmd -I
  Targets: HomeoCentrum_Dev, HomeoCentrum_Stage, HomeoCentrum_Production
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- UserMaster.RegistrationChannel
IF COL_LENGTH(N'dbo.UserMaster', N'RegistrationChannel') IS NULL
BEGIN
    ALTER TABLE dbo.UserMaster ADD RegistrationChannel NVARCHAR(40) NULL;
    PRINT 'Added UserMaster.RegistrationChannel';
END
ELSE PRINT 'UserMaster.RegistrationChannel OK';
GO

SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
IF COL_LENGTH(N'dbo.Patient', N'RegistrationChannel') IS NULL
BEGIN
    ALTER TABLE dbo.Patient ADD RegistrationChannel NVARCHAR(40) NULL;
    PRINT 'Added Patient.RegistrationChannel';
END
ELSE PRINT 'Patient.RegistrationChannel OK';
GO

SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
IF COL_LENGTH(N'dbo.Doctor', N'RegistrationChannel') IS NULL
BEGIN
    ALTER TABLE dbo.Doctor ADD RegistrationChannel NVARCHAR(40) NULL;
    PRINT 'Added Doctor.RegistrationChannel';
END
ELSE PRINT 'Doctor.RegistrationChannel OK';
GO

-- Backfill RegistrationChannel from BookingChannel / EnteredBy heuristics (best effort)
SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;

UPDATE p
SET RegistrationChannel = N'PatientApp'
FROM dbo.Patient p
WHERE p.RegistrationChannel IS NULL
  AND EXISTS (
      SELECT 1 FROM dbo.PatientAppointment a
      WHERE a.PatientId = p.PatientID AND a.BookingChannel = N'PatientApp');
PRINT CONCAT('Patient RegistrationChannel=PatientApp rows: ', @@ROWCOUNT);

UPDATE p
SET RegistrationChannel = N'WebPortal'
FROM dbo.Patient p
WHERE p.RegistrationChannel IS NULL
  AND p.EnteredBy IS NOT NULL AND LTRIM(RTRIM(p.EnteredBy)) <> N'';
PRINT CONCAT('Patient RegistrationChannel=WebPortal (had EnteredBy) rows: ', @@ROWCOUNT);
GO

-- Ensure CaseEntryDetail for every distinct DoctorId+PatientId with a non-deleted appointment
SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;

;WITH pairs AS (
    SELECT DISTINCT a.DoctorId, a.PatientId,
           MIN(a.AppointmentDate) AS FirstVisit
    FROM dbo.PatientAppointment a
    WHERE ISNULL(a.DeleteStatus, 0) = 0
      AND a.DoctorId > 0 AND a.PatientId > 0
      AND ISNULL(a.Status, N'') NOT IN (N'CANCELLED', N'Canceled', N'REJECTED')
    GROUP BY a.DoctorId, a.PatientId
)
INSERT INTO dbo.CaseEntryDetails (PatientId, DoctorId, UserId, DateodFirstVisit, DeleteStatus, EnteredBy, EnteredDate, RefBy)
SELECT
    p.PatientId,
    p.DoctorId,
    d.UserId,
    ISNULL(p.FirstVisit, GETDATE()),
    0,
    N'BackfillBooking',
    GETDATE(),
    N'BackfillBooking'
FROM pairs p
INNER JOIN dbo.Doctor d ON d.DoctorID = p.DoctorId AND ISNULL(d.DeleteStatus, 0) = 0
INNER JOIN dbo.Patient pt ON pt.PatientID = p.PatientId AND ISNULL(pt.DeleteStatus, 0) = 0
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.CaseEntryDetails c
    WHERE c.PatientId = p.PatientId
      AND c.DoctorId = p.DoctorId
      AND ISNULL(c.DeleteStatus, 0) = 0
);
-- Note: CaseEntryDetails uses PatientId/DoctorId; Patient.PatientID / Doctor.DoctorID on master tables.

PRINT CONCAT('CaseEntryDetails backfilled from appointments: ', @@ROWCOUNT);
GO

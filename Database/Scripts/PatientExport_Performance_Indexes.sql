/*
    Patient export performance indexes for Doctor Dashboard.
    Run manually on the NIGA Centrum database when convenient.
    Safe to re-run: each index is created only if it does not already exist.

    Table names match NIGACentrumContext mappings:
      - PatientAppointment (singular)
      - CaseEntryDetails
      - CaseEntryDiagnosis
      - CaseEntryChiefComplaint
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PatientAppointment_UserId_AppointmentDate'
      AND object_id = OBJECT_ID('dbo.PatientAppointment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PatientAppointment_UserId_AppointmentDate
    ON dbo.PatientAppointment (UserId, AppointmentDate)
    INCLUDE (PatientId, Status, AppointmentTime, DeleteStatus);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_CaseEntryDetails_DoctorId_DeleteStatus'
      AND object_id = OBJECT_ID('dbo.CaseEntryDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_CaseEntryDetails_DoctorId_DeleteStatus
    ON dbo.CaseEntryDetails (DoctorId, DeleteStatus)
    INCLUDE (PatientId, CaseId, DateodFirstVisit, RefBy, EnteredBy, EnteredDate, ChangedBy, ChangedDate);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_CaseEntryDiagnoses_CaseId'
      AND object_id = OBJECT_ID('dbo.CaseEntryDiagnosis')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_CaseEntryDiagnoses_CaseId
    ON dbo.CaseEntryDiagnosis (CaseId)
    INCLUDE (DiagnosisId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_CaseEntryChiefComplaint_CaseId'
      AND object_id = OBJECT_ID('dbo.CaseEntryChiefComplaint')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_CaseEntryChiefComplaint_CaseId
    ON dbo.CaseEntryChiefComplaint (CaseId)
    INCLUDE (ChiefComplaintName);
END
GO

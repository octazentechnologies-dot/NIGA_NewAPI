/*
  Appointment lifecycle: OVERDUE (24h reschedule grace) → EXPIRED (no refund / no reschedule).
  Run on HomeoCentrum_Dev and HomeoCentrum_Stage.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.PatientAppointment is missing.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.PatientAppointment', N'OverdueAt') IS NULL
BEGIN
    ALTER TABLE dbo.PatientAppointment ADD OverdueAt DATETIME NULL;
    PRINT 'Added PatientAppointment.OverdueAt';
END
ELSE
    PRINT 'PatientAppointment.OverdueAt OK';

IF COL_LENGTH(N'dbo.PatientAppointment', N'GraceEndsAt') IS NULL
BEGIN
    ALTER TABLE dbo.PatientAppointment ADD GraceEndsAt DATETIME NULL;
    PRINT 'Added PatientAppointment.GraceEndsAt';
END
ELSE
    PRINT 'PatientAppointment.GraceEndsAt OK';

IF COL_LENGTH(N'dbo.PatientAppointment', N'ExpiredAt') IS NULL
BEGIN
    ALTER TABLE dbo.PatientAppointment ADD ExpiredAt DATETIME NULL;
    PRINT 'Added PatientAppointment.ExpiredAt';
END
ELSE
    PRINT 'PatientAppointment.ExpiredAt OK';

IF COL_LENGTH(N'dbo.PatientAppointment', N'RefundEligible') IS NULL
BEGIN
    ALTER TABLE dbo.PatientAppointment ADD RefundEligible BIT NULL;
    PRINT 'Added PatientAppointment.RefundEligible';
END
ELSE
    PRINT 'PatientAppointment.RefundEligible OK';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PatientAppointment_GraceLifecycle'
      AND object_id = OBJECT_ID(N'dbo.PatientAppointment'))
BEGIN
    CREATE INDEX IX_PatientAppointment_GraceLifecycle
        ON dbo.PatientAppointment (Status, GraceEndsAt, AppointmentDate)
        WHERE DeleteStatus = 0;
    PRINT 'Created IX_PatientAppointment_GraceLifecycle';
END
ELSE
    PRINT 'IX_PatientAppointment_GraceLifecycle OK';
GO

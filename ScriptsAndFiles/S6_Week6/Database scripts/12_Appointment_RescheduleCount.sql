/*
  PatientAppointment.RescheduleCount — hard max 2 successful RescheduleAppointment calls per visit.
  Backfills from dbo.AppointmentChangeLog Action = N'Reschedule' when present.
  Run on HomeoCentrum_Dev and HomeoCentrum_Stage.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.PatientAppointment is missing.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.PatientAppointment', N'RescheduleCount') IS NULL
BEGIN
    ALTER TABLE dbo.PatientAppointment ADD RescheduleCount INT NOT NULL
        CONSTRAINT DF_PatientAppointment_RescheduleCount DEFAULT (0);
    PRINT 'Added PatientAppointment.RescheduleCount';
END
ELSE
    PRINT 'PatientAppointment.RescheduleCount OK';
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.AppointmentChangeLog', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientAppointment', N'RescheduleCount') IS NOT NULL
BEGIN
    ;WITH counts AS (
        SELECT PatientAppId, COUNT(1) AS Cnt
        FROM dbo.AppointmentChangeLog
        WHERE Action = N'Reschedule'
        GROUP BY PatientAppId
    )
    UPDATE a
    SET RescheduleCount = CASE
            WHEN c.Cnt > a.RescheduleCount THEN c.Cnt
            ELSE a.RescheduleCount
        END
    FROM dbo.PatientAppointment a
    INNER JOIN counts c ON c.PatientAppId = a.PatientAppId
    WHERE ISNULL(a.RescheduleCount, 0) < c.Cnt;

    PRINT 'Backfilled RescheduleCount from AppointmentChangeLog where higher.';
END
ELSE
    PRINT 'Skip RescheduleCount backfill (log or column missing).';
GO

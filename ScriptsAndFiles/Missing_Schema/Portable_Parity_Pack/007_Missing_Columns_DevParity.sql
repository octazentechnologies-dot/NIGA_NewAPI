/*
Purpose: Columns present on HomeoCentrum_Dev but missing on Stage after table-level parity.
Evidence: 2026-10-20 column signature diff.
Idempotent. Additive only. No data backfill invented.
Risk: Low.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.AppointmentRequest', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppointmentRequest', N'IdempotencyKey') IS NULL
BEGIN
    ALTER TABLE dbo.AppointmentRequest ADD IdempotencyKey nvarchar(80) NULL;
    PRINT 'Added AppointmentRequest.IdempotencyKey';
END
GO

IF OBJECT_ID(N'dbo.MedicineOrder', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.MedicineOrder', N'DeliveryAddressId') IS NULL
BEGIN
    ALTER TABLE dbo.MedicineOrder ADD DeliveryAddressId int NULL;
    PRINT 'Added MedicineOrder.DeliveryAddressId';
END
GO

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientAppointment', N'BookedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.PatientAppointment ADD BookedByUserId bigint NULL;
    PRINT 'Added PatientAppointment.BookedByUserId';
END
GO

IF OBJECT_ID(N'dbo.PatientReminder', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientReminder', N'NotifyPush') IS NULL
BEGIN
    ALTER TABLE dbo.PatientReminder ADD NotifyPush bit NOT NULL
        CONSTRAINT DF_PatientReminder_NotifyPush DEFAULT (0);
    PRINT 'Added PatientReminder.NotifyPush';
END
GO

IF OBJECT_ID(N'dbo.PatientReminder', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientReminder', N'SnoozeMinutes') IS NULL
BEGIN
    ALTER TABLE dbo.PatientReminder ADD SnoozeMinutes int NULL;
    PRINT 'Added PatientReminder.SnoozeMinutes';
END
GO

PRINT '007_Missing_Columns_DevParity completed.';
GO

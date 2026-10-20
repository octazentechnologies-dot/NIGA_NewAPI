/*
  S6 Week 6 — verify objects created by 01–04 and 06.
  Prints OK / MISSING for each required object or column.
  Run AFTER 01, 02, 03, 04, and 06.
*/
SET NOCOUNT ON;

SELECT Obj, Status
FROM (
    SELECT N'SmsOtpVerify' AS Obj,
           CASE WHEN OBJECT_ID(N'dbo.SmsOtpVerify', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END AS Status
    UNION ALL
    SELECT N'FirebaseNotificationLog',
           CASE WHEN OBJECT_ID(N'dbo.FirebaseNotificationLog', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'DevicePushToken',
           CASE WHEN OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'DevicePushToken.DeviceType',
           CASE WHEN COL_LENGTH(N'dbo.DevicePushToken', N'DeviceType') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'CareLinkRequest',
           CASE WHEN OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'CareLinkRequest.Opinion',
           CASE WHEN COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'CareLinkInfoRequest',
           CASE WHEN OBJECT_ID(N'dbo.CareLinkInfoRequest', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'AppNotification.Type',
           CASE WHEN OBJECT_ID(N'dbo.AppNotification', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'AppNotification.DeepLink',
           CASE WHEN OBJECT_ID(N'dbo.AppNotification', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'PatientAppointment.BookedByUserId',
           CASE WHEN OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.PatientAppointment', N'BookedByUserId') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'ErxSnapshot.LabOrdersJson',
           CASE WHEN OBJECT_ID(N'dbo.ErxSnapshot', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.ErxSnapshot', N'LabOrdersJson') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'ErxSnapshot.Notes',
           CASE WHEN OBJECT_ID(N'dbo.ErxSnapshot', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.ErxSnapshot', N'Notes') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'MedicineOrder.DeliveryAddressId',
           CASE WHEN OBJECT_ID(N'dbo.MedicineOrder', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.MedicineOrder', N'DeliveryAddressId') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'PatientDeliveryAddress',
           CASE WHEN OBJECT_ID(N'dbo.PatientDeliveryAddress', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'ReceptionVisitVitals',
           CASE WHEN OBJECT_ID(N'dbo.ReceptionVisitVitals', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'DoctorCareCategory',
           CASE WHEN OBJECT_ID(N'dbo.DoctorCareCategory', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'PublicBookingIdempotency',
           CASE WHEN OBJECT_ID(N'dbo.PublicBookingIdempotency', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'UserNotificationPreference',
           CASE WHEN OBJECT_ID(N'dbo.UserNotificationPreference', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'PatientReminder',
           CASE WHEN OBJECT_ID(N'dbo.PatientReminder', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'PatientReminder.NotifyPush',
           CASE WHEN OBJECT_ID(N'dbo.PatientReminder', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.PatientReminder', N'NotifyPush') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'DoctorTimeOff',
           CASE WHEN OBJECT_ID(N'dbo.DoctorTimeOff', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'AppointmentRequest',
           CASE WHEN OBJECT_ID(N'dbo.AppointmentRequest', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'AppointmentRequest.IdempotencyKey',
           CASE WHEN OBJECT_ID(N'dbo.AppointmentRequest', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.AppointmentRequest', N'IdempotencyKey') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'PharmacyQuery',
           CASE WHEN OBJECT_ID(N'dbo.PharmacyQuery', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'DoctorBookingRule',
           CASE WHEN OBJECT_ID(N'dbo.DoctorBookingRule', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'InstantConsultRequest.PatientAppId',
           CASE WHEN OBJECT_ID(N'dbo.InstantConsultRequest', N'U') IS NULL THEN N'SKIP(no table)'
                WHEN COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientAppId') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL
    SELECT N'OtpChallenge (S1 prerequisite)',
           CASE WHEN OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
) x
ORDER BY Obj;

DECLARE @Missing int =
    (SELECT COUNT(*) FROM (
        SELECT CASE WHEN OBJECT_ID(N'dbo.SmsOtpVerify', N'U') IS NULL THEN 1 ELSE 0 END AS m
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.FirebaseNotificationLog', N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.DevicePushToken', N'DeviceType') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NOT NULL
                              AND COL_LENGTH(N'dbo.PatientAppointment', N'BookedByUserId') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.ErxSnapshot', N'U') IS NOT NULL
                              AND COL_LENGTH(N'dbo.ErxSnapshot', N'LabOrdersJson') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.PatientDeliveryAddress', N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.ReceptionVisitVitals', N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.AppointmentRequest', N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.DoctorBookingRule', N'U') IS NULL THEN 1 ELSE 0 END
    ) t WHERE m = 1);

IF @Missing > 0
BEGIN
    RAISERROR('S6 verify FAILED: one or more required objects are missing. Run 01–04 then 06 in order.', 16, 1);
END
ELSE
    PRINT 'S6 verify PASSED: all required objects present.';
GO

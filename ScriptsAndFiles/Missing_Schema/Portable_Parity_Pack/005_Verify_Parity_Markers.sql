/*
Purpose: Verify required S6 + Mobile + extra parity objects exist on current DB.
Read-mostly SELECT; raises if any required marker MISSING.
*/
SET NOCOUNT ON;

SELECT @@SERVERNAME AS Srv, DB_NAME() AS DbName;

SELECT Marker, Status
FROM (
    SELECT N'SmsOtpVerify' AS Marker, CASE WHEN OBJECT_ID(N'dbo.SmsOtpVerify',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END AS Status
    UNION ALL SELECT N'FirebaseNotificationLog', CASE WHEN OBJECT_ID(N'dbo.FirebaseNotificationLog',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'DevicePushToken.DeviceType', CASE WHEN COL_LENGTH(N'dbo.DevicePushToken',N'DeviceType') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'CareLinkRequest', CASE WHEN OBJECT_ID(N'dbo.CareLinkRequest',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'CareLinkRequest.Opinion', CASE WHEN COL_LENGTH(N'dbo.CareLinkRequest',N'Opinion') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'CareLinkInfoRequest', CASE WHEN OBJECT_ID(N'dbo.CareLinkInfoRequest',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'AppNotification.Type', CASE WHEN COL_LENGTH(N'dbo.AppNotification',N'Type') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'AppNotification.DeepLink', CASE WHEN COL_LENGTH(N'dbo.AppNotification',N'DeepLink') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'InstantConsultRequest.PatientAppId', CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest',N'PatientAppId') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'InstantConsultRequest.TeleSessionId', CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest',N'TeleSessionId') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'UserNotificationPreference', CASE WHEN OBJECT_ID(N'dbo.UserNotificationPreference',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'PatientReminder', CASE WHEN OBJECT_ID(N'dbo.PatientReminder',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'DoctorTimeOff', CASE WHEN OBJECT_ID(N'dbo.DoctorTimeOff',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'AppointmentRequest', CASE WHEN OBJECT_ID(N'dbo.AppointmentRequest',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'PharmacyQuery', CASE WHEN OBJECT_ID(N'dbo.PharmacyQuery',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'DoctorBookingRule', CASE WHEN OBJECT_ID(N'dbo.DoctorBookingRule',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'PatientDeliveryAddress', CASE WHEN OBJECT_ID(N'dbo.PatientDeliveryAddress',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'DoctorCareCategory', CASE WHEN OBJECT_ID(N'dbo.DoctorCareCategory',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'PublicBookingIdempotency', CASE WHEN OBJECT_ID(N'dbo.PublicBookingIdempotency',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'ReceptionVisitVitals', CASE WHEN OBJECT_ID(N'dbo.ReceptionVisitVitals',N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
) x
ORDER BY Marker;

DECLARE @Missing int =
(
    SELECT COUNT(*) FROM (
        SELECT CASE WHEN OBJECT_ID(N'dbo.SmsOtpVerify',N'U') IS NULL THEN 1 ELSE 0 END AS m
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.FirebaseNotificationLog',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.DevicePushToken',N'DeviceType') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.CareLinkRequest',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.CareLinkInfoRequest',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.AppNotification',N'Type') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.AppNotification',N'DeepLink') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest',N'PatientAppId') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest',N'TeleSessionId') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.UserNotificationPreference',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.PatientReminder',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.DoctorTimeOff',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.AppointmentRequest',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.PharmacyQuery',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.DoctorBookingRule',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.PatientDeliveryAddress',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.DoctorCareCategory',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.PublicBookingIdempotency',N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.ReceptionVisitVitals',N'U') IS NULL THEN 1 ELSE 0 END
    ) t WHERE m = 1
);

SELECT CASE WHEN @Missing = 0 THEN N'PASS' ELSE N'INCOMPLETE' END AS Parity_Result, @Missing AS MissingCount;

IF @Missing > 0
    RAISERROR('Parity verify FAILED: one or more required objects are missing. Run Portable_Parity_Pack 001-004.', 16, 1);
ELSE
    PRINT 'Parity verify PASSED.';
GO

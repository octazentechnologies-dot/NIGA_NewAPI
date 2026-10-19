/*
  S6 Week 6 — verify objects created by 01–04.
  Prints OK / MISSING for each required object or column.
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
    ) t WHERE m = 1);

IF @Missing > 0
BEGIN
    RAISERROR('S6 verify FAILED: one or more required objects are missing. Run 01–04 in order.', 16, 1);
END
ELSE
    PRINT 'S6 verify PASSED: all required objects present.';
GO

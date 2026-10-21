/*
Purpose: Read-only S6 (+ InstantConsult column) parity markers for the current database.
Source evidence: S6_Week6 scripts 01–05; S3 12_InstantConsult_VisitLink.sql (PatientAppId/TeleSessionId).
Prerequisites: Connected to HomeoCentrum_Dev or HomeoCentrum_Stage only.
Target environments: Dev, Stage (verify-only).
Risk: None (SELECT only).
Failure behavior: Raises if DB_NAME not Dev/Stage.
Rollback: N/A.
*/
SET NOCOUNT ON;
DECLARE @Db sysname = DB_NAME();
IF @Db NOT IN (N'HomeoCentrum_Dev', N'HomeoCentrum_Stage')
BEGIN
    DECLARE @Msg nvarchar(400) = N'Refuse: connect to HomeoCentrum_Dev or HomeoCentrum_Stage only. Current=' + @Db;
    RAISERROR(@Msg, 16, 1);
    RETURN;
END;

SELECT @@SERVERNAME AS Srv, DB_NAME() AS DbName;

SELECT Marker, Status
FROM (
    SELECT N'SmsOtpVerify' AS Marker,
           CASE WHEN OBJECT_ID(N'dbo.SmsOtpVerify', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END AS Status
    UNION ALL SELECT N'FirebaseNotificationLog',
           CASE WHEN OBJECT_ID(N'dbo.FirebaseNotificationLog', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'DevicePushToken.DeviceType',
           CASE WHEN COL_LENGTH(N'dbo.DevicePushToken', N'DeviceType') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'CareLinkRequest',
           CASE WHEN OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'CareLinkRequest.Opinion',
           CASE WHEN COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'CareLinkInfoRequest',
           CASE WHEN OBJECT_ID(N'dbo.CareLinkInfoRequest', N'U') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'AppNotification.Type',
           CASE WHEN COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'AppNotification.DeepLink',
           CASE WHEN COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'InstantConsultRequest.PatientAppId',
           CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientAppId') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'InstantConsultRequest.TeleSessionId',
           CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest', N'TeleSessionId') IS NULL THEN N'MISSING' ELSE N'OK' END
    UNION ALL SELECT N'FollowUpTask.CompletedAt',
           CASE WHEN COL_LENGTH(N'dbo.FollowUpTask', N'CompletedAt') IS NULL THEN N'MISSING' ELSE N'OK' END
) x
ORDER BY Marker;

DECLARE @Missing int =
(
    SELECT COUNT(*) FROM (
        SELECT CASE WHEN OBJECT_ID(N'dbo.SmsOtpVerify', N'U') IS NULL THEN 1 ELSE 0 END AS m
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.FirebaseNotificationLog', N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.DevicePushToken', N'DeviceType') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN OBJECT_ID(N'dbo.CareLinkInfoRequest', N'U') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientAppId') IS NULL THEN 1 ELSE 0 END
        UNION ALL SELECT CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest', N'TeleSessionId') IS NULL THEN 1 ELSE 0 END
    ) t WHERE m = 1
);

SELECT CASE WHEN @Missing = 0 THEN N'PASS' ELSE N'INCOMPLETE' END AS S6_Parity_Result, @Missing AS MissingCount;
GO

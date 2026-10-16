/* Instant accept stores the visit and waiting-room ids it opens. */
IF COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientAppId') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD PatientAppId INT NULL;
GO
IF COL_LENGTH(N'dbo.InstantConsultRequest', N'TeleSessionId') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD TeleSessionId INT NULL;
GO

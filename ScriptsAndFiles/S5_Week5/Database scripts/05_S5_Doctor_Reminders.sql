/*
================================================================================
Created      : 06-10-2026
Script       : 05_S5_Doctor_Reminders.sql
Purpose      : Doctor dashboard "Today's Reminders" card. Stores per-doctor
               reminders (date, optional time, title, description, contact).
               Soft delete via IsDeleted. Served by:
                 GET    /api/Doctor/Reminders?from=&to=
                 POST   /api/Doctor/Reminders
                 PUT    /api/Doctor/Reminders/{id}
                 DELETE /api/Doctor/Reminders/{id}
Use          : HomeoCentrum_Dev / HomeoCentrum_Stage. Idempotent. Manual run.
               Run after 01–04 in this folder.
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.DoctorReminder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorReminder
    (
        DoctorReminderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DoctorReminder PRIMARY KEY,
        DoctorId         INT            NOT NULL,
        UserId           BIGINT         NULL,
        ReminderDate     DATE           NOT NULL,
        ReminderTime     TIME(0)        NULL,
        Title            NVARCHAR(200)  NOT NULL,
        Description      NVARCHAR(1000) NULL,
        ContactNumber    NVARCHAR(20)   NULL,
        IsDone           BIT            NOT NULL CONSTRAINT DF_DoctorReminder_IsDone DEFAULT (0),
        IsDeleted        BIT            NOT NULL CONSTRAINT DF_DoctorReminder_IsDeleted DEFAULT (0),
        CreatedAt        DATETIME2(0)   NOT NULL CONSTRAINT DF_DoctorReminder_CreatedAt DEFAULT (SYSDATETIME()),
        UpdatedAt        DATETIME2(0)   NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DoctorReminder_Doctor_Date' AND object_id = OBJECT_ID(N'dbo.DoctorReminder'))
BEGIN
    CREATE INDEX IX_DoctorReminder_Doctor_Date
        ON dbo.DoctorReminder (DoctorId, ReminderDate)
        INCLUDE (ReminderTime, IsDone, IsDeleted);
END
GO

IF OBJECT_ID(N'dbo.Doctor', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DoctorReminder_Doctor')
BEGIN
    ALTER TABLE dbo.DoctorReminder WITH CHECK
        ADD CONSTRAINT FK_DoctorReminder_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.Doctor (DoctorId);
END
GO

SELECT
    OBJECT_ID(N'dbo.DoctorReminder') AS DoctorReminderTable,
    (SELECT COUNT(*) FROM dbo.DoctorReminder) AS ReminderRows;
GO

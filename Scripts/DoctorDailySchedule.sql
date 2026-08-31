-- Run once on the NIGA Centrum database before using appointment time slots.
IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'DoctorDailySchedule'
)
BEGIN
    CREATE TABLE dbo.DoctorDailySchedule (
        DoctorDailyScheduleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DoctorId INT NOT NULL,
        ScheduleDate DATE NOT NULL,
        SlotIntervalMinutes INT NOT NULL,
        WorkStartTime TIME(0) NOT NULL,
        WorkEndTime TIME(0) NOT NULL,
        CreatedByUserId BIGINT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_DoctorDailySchedule_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_DoctorDailySchedule_Doctor_Date UNIQUE (DoctorId, ScheduleDate),
        CONSTRAINT FK_DoctorDailySchedule_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.Doctor(DoctorId)
    );
END

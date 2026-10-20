-- Mobile clinic tables used by NIGA_NewAPI MobilePatient, MobileDoctor, and MobileShared controllers.
-- Safe to run more than once. Does not insert sample rows.
-- The API also creates these tables on first use. Run this script when you want the tables before that call.

IF OBJECT_ID(N'dbo.UserNotificationPreference', N'U') IS NULL
CREATE TABLE dbo.UserNotificationPreference (
    UserNotificationPreferenceId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId bigint NOT NULL,
    PushEnabled bit NOT NULL CONSTRAINT DF_Unp_Push DEFAULT (1),
    SmsEnabled bit NOT NULL CONSTRAINT DF_Unp_Sms DEFAULT (0),
    EmailEnabled bit NOT NULL CONSTRAINT DF_Unp_Email DEFAULT (0),
    QuietStart time NULL,
    QuietEnd time NULL,
    Appointments bit NOT NULL CONSTRAINT DF_Unp_Appt DEFAULT (1),
    Prescriptions bit NOT NULL CONSTRAINT DF_Unp_Rx DEFAULT (1),
    Medicines bit NOT NULL CONSTRAINT DF_Unp_Med DEFAULT (1),
    Marketing bit NOT NULL CONSTRAINT DF_Unp_Mkt DEFAULT (0),
    QuietHoursEnabled bit NOT NULL CONSTRAINT DF_Unp_Quiet DEFAULT (0),
    ReminderOffsets nvarchar(100) NULL
);
GO

IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Appointments') IS NULL
    ALTER TABLE dbo.UserNotificationPreference ADD Appointments bit NOT NULL CONSTRAINT DF_Unp_Appt2 DEFAULT (1);
IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Prescriptions') IS NULL
    ALTER TABLE dbo.UserNotificationPreference ADD Prescriptions bit NOT NULL CONSTRAINT DF_Unp_Rx2 DEFAULT (1);
IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Medicines') IS NULL
    ALTER TABLE dbo.UserNotificationPreference ADD Medicines bit NOT NULL CONSTRAINT DF_Unp_Med2 DEFAULT (1);
IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Marketing') IS NULL
    ALTER TABLE dbo.UserNotificationPreference ADD Marketing bit NOT NULL CONSTRAINT DF_Unp_Mkt2 DEFAULT (0);
IF COL_LENGTH(N'dbo.UserNotificationPreference', N'QuietHoursEnabled') IS NULL
    ALTER TABLE dbo.UserNotificationPreference ADD QuietHoursEnabled bit NOT NULL CONSTRAINT DF_Unp_Quiet2 DEFAULT (0);
IF COL_LENGTH(N'dbo.UserNotificationPreference', N'ReminderOffsets') IS NULL
    ALTER TABLE dbo.UserNotificationPreference ADD ReminderOffsets nvarchar(100) NULL;
GO

IF OBJECT_ID(N'dbo.PatientReminder', N'U') IS NULL
CREATE TABLE dbo.PatientReminder (
    PatientReminderId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId bigint NOT NULL,
    PatientId int NOT NULL,
    Title nvarchar(200) NOT NULL,
    DueAt datetime NOT NULL,
    IsDone bit NOT NULL CONSTRAINT DF_Pr_Done DEFAULT (0),
    CreatedAt datetime NOT NULL
);
GO

IF OBJECT_ID(N'dbo.DoctorTimeOff', N'U') IS NULL
CREATE TABLE dbo.DoctorTimeOff (
    DoctorTimeOffId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DoctorId int NOT NULL,
    StartsAt datetime NOT NULL,
    EndsAt datetime NOT NULL,
    Reason nvarchar(300) NULL
);
GO

IF OBJECT_ID(N'dbo.AppointmentRequest', N'U') IS NULL
CREATE TABLE dbo.AppointmentRequest (
    AppointmentRequestId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DoctorId int NOT NULL,
    PatientId int NOT NULL,
    RequestedDate datetime NOT NULL,
    RequestedTime nvarchar(8) NOT NULL,
    ConsultMode nvarchar(20) NULL,
    Status nvarchar(20) NOT NULL,
    Reason nvarchar(500) NULL,
    ProposedDate datetime NULL,
    ProposedTime nvarchar(8) NULL,
    PatientAppId int NULL,
    CreatedAt datetime NOT NULL
);
GO

IF OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NULL
CREATE TABLE dbo.CareLinkRequest (
    CareLinkRequestId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FromDoctorId int NOT NULL,
    ToDoctorId int NOT NULL,
    PatientId int NOT NULL,
    Note nvarchar(500) NULL,
    FeeAmount decimal(18,2) NULL,
    Status nvarchar(20) NOT NULL,
    CreatedAt datetime NOT NULL
);
GO

IF OBJECT_ID(N'dbo.PharmacyQuery', N'U') IS NULL
CREATE TABLE dbo.PharmacyQuery (
    PharmacyQueryId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DoctorId int NOT NULL,
    PatientId int NOT NULL,
    Question nvarchar(1000) NOT NULL,
    Reply nvarchar(1000) NULL,
    Status nvarchar(20) NOT NULL,
    CreatedAt datetime NOT NULL
);
GO

IF OBJECT_ID(N'dbo.DoctorBookingRule', N'U') IS NULL
CREATE TABLE dbo.DoctorBookingRule (
    DoctorId int NOT NULL PRIMARY KEY,
    GapMinutes int NULL,
    BookingHorizonDays int NULL
);
GO

IF COL_LENGTH(N'dbo.PatientReminder', N'Kind') IS NULL
    ALTER TABLE dbo.PatientReminder ADD Kind nvarchar(40) NULL;
IF COL_LENGTH(N'dbo.PatientReminder', N'TimeOfDay') IS NULL
    ALTER TABLE dbo.PatientReminder ADD TimeOfDay nvarchar(8) NULL;
IF COL_LENGTH(N'dbo.PatientReminder', N'Weekdays') IS NULL
    ALTER TABLE dbo.PatientReminder ADD Weekdays nvarchar(40) NULL;
IF COL_LENGTH(N'dbo.PatientReminder', N'MemberPatientId') IS NULL
    ALTER TABLE dbo.PatientReminder ADD MemberPatientId int NULL;
GO

IF COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NULL
    ALTER TABLE dbo.CareLinkRequest ADD Opinion nvarchar(1000) NULL;
GO

IF COL_LENGTH(N'dbo.ErxSnapshot', N'Notes') IS NULL
    ALTER TABLE dbo.ErxSnapshot ADD Notes nvarchar(1000) NULL;
GO

/*
  S6 Week 6 — runtime / development schema gaps (idempotent).

  Covers columns & tables the New API creates on first use (Ensure* helpers)
  that are NOT already applied by S6 scripts 01–04, and that cause production
  errors when EF / FromSql runs before those code paths (e.g. Invalid column
  name 'BookedByUserId', missing LabOrdersJson on Timeline).

  Safe to re-run. Prerequisites: S1–S5 base objects already on the database
  (PatientAppointment, ErxSnapshot, MedicineOrder, AppNotification, etc.).

  Run AFTER 01–04, THEN run 05_Verify_S6_Objects.sql.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT '=== S6 06: Runtime / Dev schema gaps ===';

/* ------------------------------------------------------------------ */
/* 1) CRITICAL: PatientAppointment.BookedByUserId (EF-mapped)         */
/* ------------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientAppointment', N'BookedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.PatientAppointment ADD BookedByUserId BIGINT NULL;
    PRINT 'Added PatientAppointment.BookedByUserId';
END
ELSE
    PRINT 'PatientAppointment.BookedByUserId OK / skipped';

/* ------------------------------------------------------------------ */
/* 2) ErxSnapshot.LabOrdersJson + Notes (Timeline / eRx FromSql)      */
/* ------------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.ErxSnapshot', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ErxSnapshot', N'LabOrdersJson') IS NULL
    BEGIN
        ALTER TABLE dbo.ErxSnapshot ADD LabOrdersJson NVARCHAR(MAX) NULL;
        PRINT 'Added ErxSnapshot.LabOrdersJson';
    END
    ELSE
        PRINT 'ErxSnapshot.LabOrdersJson OK';

    IF COL_LENGTH(N'dbo.ErxSnapshot', N'Notes') IS NULL
    BEGIN
        ALTER TABLE dbo.ErxSnapshot ADD Notes NVARCHAR(1000) NULL;
        PRINT 'Added ErxSnapshot.Notes';
    END
    ELSE
        PRINT 'ErxSnapshot.Notes OK';
END
ELSE
    PRINT 'dbo.ErxSnapshot missing — run S4 schema first; skipped LabOrdersJson/Notes';

/* ------------------------------------------------------------------ */
/* 3) MedicineOrder.DeliveryAddressId + PatientDeliveryAddress        */
/* ------------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.MedicineOrder', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.MedicineOrder', N'DeliveryAddressId') IS NULL
BEGIN
    ALTER TABLE dbo.MedicineOrder ADD DeliveryAddressId INT NULL;
    PRINT 'Added MedicineOrder.DeliveryAddressId';
END
ELSE
    PRINT 'MedicineOrder.DeliveryAddressId OK / skipped';

IF OBJECT_ID(N'dbo.PatientDeliveryAddress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientDeliveryAddress (
        DeliveryAddressId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PatientId INT NOT NULL,
        Label NVARCHAR(80) NULL,
        Line1 NVARCHAR(200) NOT NULL,
        Line2 NVARCHAR(200) NULL,
        City NVARCHAR(80) NULL,
        State NVARCHAR(80) NULL,
        Pincode NVARCHAR(12) NOT NULL,
        Phone NVARCHAR(30) NULL,
        IsDefault BIT NOT NULL CONSTRAINT DF_Pda_Default DEFAULT (0),
        CreatedAt DATETIME NOT NULL
    );
    PRINT 'Created dbo.PatientDeliveryAddress';
END
ELSE
BEGIN
    PRINT 'dbo.PatientDeliveryAddress already exists';
    IF COL_LENGTH(N'dbo.PatientDeliveryAddress', N'Phone') IS NULL
    BEGIN
        ALTER TABLE dbo.PatientDeliveryAddress ADD Phone NVARCHAR(30) NULL;
        PRINT 'Added PatientDeliveryAddress.Phone';
    END
END

/* ------------------------------------------------------------------ */
/* 4) ReceptionVisitVitals                                            */
/* ------------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.ReceptionVisitVitals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceptionVisitVitals (
        ReceptionVisitVitalsId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceptionVisitVitals PRIMARY KEY,
        PatientAppId INT NOT NULL,
        PatientId INT NOT NULL,
        DoctorId INT NOT NULL,
        VitalsJson NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ReceptionVisitVitals_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_ReceptionVisitVitals_UpdatedAt DEFAULT (GETDATE()),
        UpdatedBy BIGINT NULL
    );
    CREATE UNIQUE INDEX UX_ReceptionVisitVitals_PatientAppId ON dbo.ReceptionVisitVitals (PatientAppId);
    PRINT 'Created dbo.ReceptionVisitVitals';
END
ELSE
    PRINT 'dbo.ReceptionVisitVitals already exists';

/* ------------------------------------------------------------------ */
/* 5) Public booking helpers                                          */
/* ------------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.DoctorCareCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorCareCategory (
        DoctorId INT NOT NULL,
        CareCategoryId INT NOT NULL,
        CONSTRAINT PK_DoctorCareCategory PRIMARY KEY (DoctorId, CareCategoryId)
    );
    PRINT 'Created dbo.DoctorCareCategory';
END
ELSE
    PRINT 'dbo.DoctorCareCategory already exists';

IF OBJECT_ID(N'dbo.PublicBookingIdempotency', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PublicBookingIdempotency (
        PublicBookingIdempotencyId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IdempotencyKey NVARCHAR(80) NOT NULL,
        DoctorId INT NOT NULL,
        PatientMobile NVARCHAR(20) NOT NULL,
        BodyFingerprint NVARCHAR(200) NOT NULL,
        PatientAppId INT NOT NULL,
        BookingToken NVARCHAR(64) NOT NULL,
        CreatedAt DATETIME2 NOT NULL
    );
    PRINT 'Created dbo.PublicBookingIdempotency';
END
ELSE
    PRINT 'dbo.PublicBookingIdempotency already exists';

/* ------------------------------------------------------------------ */
/* 6) Mobile clinic pack (API EnsureSchemaAsync)                      */
/* ------------------------------------------------------------------ */
IF OBJECT_ID(N'dbo.UserNotificationPreference', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserNotificationPreference (
        UserNotificationPreferenceId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId BIGINT NOT NULL,
        PushEnabled BIT NOT NULL CONSTRAINT DF_Unp_Push DEFAULT (1),
        SmsEnabled BIT NOT NULL CONSTRAINT DF_Unp_Sms DEFAULT (0),
        EmailEnabled BIT NOT NULL CONSTRAINT DF_Unp_Email DEFAULT (0),
        QuietStart TIME NULL,
        QuietEnd TIME NULL,
        Appointments BIT NOT NULL CONSTRAINT DF_Unp_Appt DEFAULT (1),
        Prescriptions BIT NOT NULL CONSTRAINT DF_Unp_Rx DEFAULT (1),
        Medicines BIT NOT NULL CONSTRAINT DF_Unp_Med DEFAULT (1),
        Marketing BIT NOT NULL CONSTRAINT DF_Unp_Mkt DEFAULT (0),
        QuietHoursEnabled BIT NOT NULL CONSTRAINT DF_Unp_Quiet DEFAULT (0),
        ReminderOffsets NVARCHAR(100) NULL
    );
    PRINT 'Created dbo.UserNotificationPreference';
END
ELSE
BEGIN
    PRINT 'dbo.UserNotificationPreference already exists';
    IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Appointments') IS NULL
        ALTER TABLE dbo.UserNotificationPreference ADD Appointments BIT NOT NULL CONSTRAINT DF_Unp_Appt2 DEFAULT (1);
    IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Prescriptions') IS NULL
        ALTER TABLE dbo.UserNotificationPreference ADD Prescriptions BIT NOT NULL CONSTRAINT DF_Unp_Rx2 DEFAULT (1);
    IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Medicines') IS NULL
        ALTER TABLE dbo.UserNotificationPreference ADD Medicines BIT NOT NULL CONSTRAINT DF_Unp_Med2 DEFAULT (1);
    IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Marketing') IS NULL
        ALTER TABLE dbo.UserNotificationPreference ADD Marketing BIT NOT NULL CONSTRAINT DF_Unp_Mkt2 DEFAULT (0);
    IF COL_LENGTH(N'dbo.UserNotificationPreference', N'QuietHoursEnabled') IS NULL
        ALTER TABLE dbo.UserNotificationPreference ADD QuietHoursEnabled BIT NOT NULL CONSTRAINT DF_Unp_Quiet2 DEFAULT (0);
    IF COL_LENGTH(N'dbo.UserNotificationPreference', N'ReminderOffsets') IS NULL
        ALTER TABLE dbo.UserNotificationPreference ADD ReminderOffsets NVARCHAR(100) NULL;
END

IF OBJECT_ID(N'dbo.PatientReminder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientReminder (
        PatientReminderId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId BIGINT NOT NULL,
        PatientId INT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        DueAt DATETIME NOT NULL,
        IsDone BIT NOT NULL CONSTRAINT DF_Pr_Done DEFAULT (0),
        CreatedAt DATETIME NOT NULL
    );
    PRINT 'Created dbo.PatientReminder';
END
ELSE
    PRINT 'dbo.PatientReminder already exists';

IF COL_LENGTH(N'dbo.PatientReminder', N'Kind') IS NULL
    ALTER TABLE dbo.PatientReminder ADD Kind NVARCHAR(40) NULL;
IF COL_LENGTH(N'dbo.PatientReminder', N'TimeOfDay') IS NULL
    ALTER TABLE dbo.PatientReminder ADD TimeOfDay NVARCHAR(8) NULL;
IF COL_LENGTH(N'dbo.PatientReminder', N'Weekdays') IS NULL
    ALTER TABLE dbo.PatientReminder ADD Weekdays NVARCHAR(40) NULL;
IF COL_LENGTH(N'dbo.PatientReminder', N'MemberPatientId') IS NULL
    ALTER TABLE dbo.PatientReminder ADD MemberPatientId INT NULL;
IF COL_LENGTH(N'dbo.PatientReminder', N'SnoozeMinutes') IS NULL
    ALTER TABLE dbo.PatientReminder ADD SnoozeMinutes INT NULL;
IF COL_LENGTH(N'dbo.PatientReminder', N'NotifyPush') IS NULL
    ALTER TABLE dbo.PatientReminder ADD NotifyPush BIT NOT NULL CONSTRAINT DF_Pr_NotifyPush DEFAULT (0);

IF OBJECT_ID(N'dbo.DoctorTimeOff', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorTimeOff (
        DoctorTimeOffId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DoctorId INT NOT NULL,
        StartsAt DATETIME NOT NULL,
        EndsAt DATETIME NOT NULL,
        Reason NVARCHAR(300) NULL
    );
    PRINT 'Created dbo.DoctorTimeOff';
END
ELSE
    PRINT 'dbo.DoctorTimeOff already exists';

IF OBJECT_ID(N'dbo.AppointmentRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppointmentRequest (
        AppointmentRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DoctorId INT NOT NULL,
        PatientId INT NOT NULL,
        RequestedDate DATETIME NOT NULL,
        RequestedTime NVARCHAR(8) NOT NULL,
        ConsultMode NVARCHAR(20) NULL,
        Status NVARCHAR(20) NOT NULL,
        Reason NVARCHAR(500) NULL,
        ProposedDate DATETIME NULL,
        ProposedTime NVARCHAR(8) NULL,
        PatientAppId INT NULL,
        CreatedAt DATETIME NOT NULL
    );
    PRINT 'Created dbo.AppointmentRequest';
END
ELSE
    PRINT 'dbo.AppointmentRequest already exists';

IF OBJECT_ID(N'dbo.AppointmentRequest', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.AppointmentRequest', N'IdempotencyKey') IS NULL
BEGIN
    ALTER TABLE dbo.AppointmentRequest ADD IdempotencyKey NVARCHAR(80) NULL;
    PRINT 'Added AppointmentRequest.IdempotencyKey';
END

IF OBJECT_ID(N'dbo.PharmacyQuery', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PharmacyQuery (
        PharmacyQueryId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DoctorId INT NOT NULL,
        PatientId INT NOT NULL,
        Question NVARCHAR(1000) NOT NULL,
        Reply NVARCHAR(1000) NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME NOT NULL
    );
    PRINT 'Created dbo.PharmacyQuery';
END
ELSE
    PRINT 'dbo.PharmacyQuery already exists';

IF OBJECT_ID(N'dbo.DoctorBookingRule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorBookingRule (
        DoctorId INT NOT NULL PRIMARY KEY,
        GapMinutes INT NULL,
        BookingHorizonDays INT NULL
    );
    PRINT 'Created dbo.DoctorBookingRule';
END
ELSE
    PRINT 'dbo.DoctorBookingRule already exists';

/* CareLink already in 03/04 — keep ALTER idempotent here too */
IF OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CareLinkRequest (
        CareLinkRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FromDoctorId INT NOT NULL,
        ToDoctorId INT NOT NULL,
        PatientId INT NOT NULL,
        Note NVARCHAR(500) NULL,
        FeeAmount DECIMAL(18,2) NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME NOT NULL
    );
    PRINT 'Created dbo.CareLinkRequest';
END

IF OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NULL
    ALTER TABLE dbo.CareLinkRequest ADD Opinion NVARCHAR(1000) NULL;

IF OBJECT_ID(N'dbo.CareLinkInfoRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CareLinkInfoRequest (
        CareLinkInfoRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CareLinkRequestId INT NOT NULL,
        FromDoctorId INT NOT NULL,
        OptionsJson NVARCHAR(1000) NULL,
        Note NVARCHAR(1000) NULL,
        CreatedAt DATETIME NOT NULL
    );
    PRINT 'Created dbo.CareLinkInfoRequest';
END

/* AppNotification Type/DeepLink already in 03/04 */
IF OBJECT_ID(N'dbo.AppNotification', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL
        ALTER TABLE dbo.AppNotification ADD Type NVARCHAR(40) NULL;
    IF COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL
        ALTER TABLE dbo.AppNotification ADD DeepLink NVARCHAR(300) NULL;
END

/* Instant consult link columns (also in 04) */
IF OBJECT_ID(N'dbo.InstantConsultRequest', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientAppId') IS NULL
        ALTER TABLE dbo.InstantConsultRequest ADD PatientAppId INT NULL;
    IF COL_LENGTH(N'dbo.InstantConsultRequest', N'TeleSessionId') IS NULL
        ALTER TABLE dbo.InstantConsultRequest ADD TeleSessionId INT NULL;
END

IF OBJECT_ID(N'dbo.FollowUpTask', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.FollowUpTask', N'CompletedAt') IS NULL
    ALTER TABLE dbo.FollowUpTask ADD CompletedAt DATETIME NULL;

PRINT '=== S6 06 done. Run 05_Verify_S6_Objects.sql next. ===';
GO

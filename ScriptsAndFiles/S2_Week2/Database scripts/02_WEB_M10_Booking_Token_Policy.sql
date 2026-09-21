/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 02_WEB_M10_Booking_Token_Policy.sql
Purpose      : WEB-04.01 booking token / visit / tele / payment placeholders on
               PatientAppointment. WEB-07/08 PolicyVersion for privacy + terms.
Use          : Run after 01. Reuses Patient + Appointment. PatientAuth OTP reuses
               dbo.OtpChallenge (Action = PatientAuth) — no extra OTP table.
Prerequisites: dbo.PatientAppointment exists. dbo.OtpChallenge from S1 script 01.
Idempotent   : Yes.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
BEGIN
    RAISERROR('dbo.PatientAppointment is missing. Stop.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.PatientAppointment', N'BookingToken') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD BookingToken NVARCHAR(64) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'VisitType') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD VisitType NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'ConsultMode') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD ConsultMode NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'PaymentStatus') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD PaymentStatus NVARCHAR(30) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'IsTele') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD IsTele BIT NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'HoldExpiresAt') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD HoldExpiresAt DATETIME NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'ConsentPolicyVersion') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD ConsentPolicyVersion NVARCHAR(20) NULL;
GO

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientAppointment', N'BookingToken') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PatientAppointment_BookingToken_S2' AND object_id = OBJECT_ID(N'dbo.PatientAppointment'))
    CREATE UNIQUE INDEX UX_PatientAppointment_BookingToken_S2
        ON dbo.PatientAppointment (BookingToken)
        WHERE BookingToken IS NOT NULL;
GO

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PatientAppointment_Doctor_Date_S2' AND object_id = OBJECT_ID(N'dbo.PatientAppointment'))
    CREATE INDEX IX_PatientAppointment_Doctor_Date_S2
        ON dbo.PatientAppointment (DoctorId, AppointmentDate, DeleteStatus);
GO

-- WEB-07 / WEB-08 — signed policy versions (content can be replaced later)
IF OBJECT_ID(N'dbo.PolicyVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PolicyVersion
    (
        PolicyVersionId INT IDENTITY(1,1) NOT NULL,
        PolicyType NVARCHAR(30) NOT NULL,
        Version NVARCHAR(20) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        BodyHtml NVARCHAR(MAX) NULL,
        EffectiveAt DATETIME NOT NULL CONSTRAINT DF_PolicyVersion_EffectiveAt DEFAULT (GETUTCDATE()),
        IsCurrent BIT NOT NULL CONSTRAINT DF_PolicyVersion_IsCurrent DEFAULT (1),
        CONSTRAINT PK_PolicyVersion PRIMARY KEY CLUSTERED (PolicyVersionId)
    );
END
GO

IF OBJECT_ID(N'dbo.PolicyVersion', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.PolicyVersion (PolicyType, Version, Title, BodyHtml, IsCurrent)
    SELECT v.PolicyType, v.Version, v.Title, v.BodyHtml, 1
    FROM (VALUES
        (N'Privacy', N'2026.09', N'Privacy policy', N'<p>Homeocentrum processes clinical, booking and pharmacy data only for care delivery. Recording and pharmacy share require explicit consent.</p>'),
        (N'Terms', N'2026.09', N'Terms of service', N'<p>Payments, refunds, telemedicine and medicine orders are governed by these terms. Booking requires acceptance of the current version.</p>')
    ) v(PolicyType, Version, Title, BodyHtml)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.PolicyVersion p
        WHERE p.PolicyType = v.PolicyType AND p.Version = v.Version
    );
END
GO

PRINT '02_WEB_M10_Booking_Token_Policy.sql complete';
GO

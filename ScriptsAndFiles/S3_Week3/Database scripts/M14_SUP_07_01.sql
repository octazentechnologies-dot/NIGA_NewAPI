/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M14_SUP_07_01.sql
Purpose      : SUP-07.01 — assisted booking DB flags:
               1) dbo.PatientAppointment.BookingChannel (value Assisted for staff-on-behalf).
               2) AssistedRequest ticket type on dbo.SupportTicket (Category / TicketType).
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.PatientAppointment, dbo.SupportTicket (SUP-01.01).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- BookingChannel on appointments ---------- */
IF COL_LENGTH(N'dbo.PatientAppointment', N'BookingChannel') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD BookingChannel NVARCHAR(30) NULL;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PatientAppointment')
      AND name = N'IX_PatientAppointment_BookingChannel'
)
    CREATE INDEX IX_PatientAppointment_BookingChannel
        ON dbo.PatientAppointment (BookingChannel)
        WHERE BookingChannel IS NOT NULL;
GO

/* ---------- Ticket type lookup (AssistedRequest) ---------- */
IF OBJECT_ID(N'dbo.SupportTicketType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportTicketType
    (
        TicketTypeCode NVARCHAR(40) NOT NULL CONSTRAINT PK_SupportTicketType PRIMARY KEY,
        DisplayName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SupportTicketType_IsActive DEFAULT (1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SupportTicketType WHERE TicketTypeCode = N'General')
    INSERT INTO dbo.SupportTicketType (TicketTypeCode, DisplayName) VALUES (N'General', N'General support');
IF NOT EXISTS (SELECT 1 FROM dbo.SupportTicketType WHERE TicketTypeCode = N'AssistedRequest')
    INSERT INTO dbo.SupportTicketType (TicketTypeCode, DisplayName) VALUES (N'AssistedRequest', N'Assisted booking request');
IF NOT EXISTS (SELECT 1 FROM dbo.SupportTicketType WHERE TicketTypeCode = N'booking')
    INSERT INTO dbo.SupportTicketType (TicketTypeCode, DisplayName) VALUES (N'booking', N'Booking help');
GO

/* TicketType on SupportTicket — AssistedRequest is a first-class type */
IF OBJECT_ID(N'dbo.SupportTicket', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.SupportTicket', N'TicketType') IS NULL
    ALTER TABLE dbo.SupportTicket ADD TicketType NVARCHAR(40) NOT NULL
        CONSTRAINT DF_SupportTicket_TicketType DEFAULT (N'General');
GO

-- Backfill: Category AssistedRequest → TicketType
IF COL_LENGTH(N'dbo.SupportTicket', N'TicketType') IS NOT NULL
BEGIN
    UPDATE dbo.SupportTicket
    SET TicketType = N'AssistedRequest'
    WHERE Category = N'AssistedRequest'
      AND (TicketType IS NULL OR TicketType = N'General' OR TicketType = N'');

    UPDATE dbo.SupportTicket
    SET TicketType = N'General'
    WHERE TicketType IS NULL OR LTRIM(RTRIM(TicketType)) = N'';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SupportTicket')
      AND name = N'IX_SupportTicket_TicketType'
)
   AND COL_LENGTH(N'dbo.SupportTicket', N'TicketType') IS NOT NULL
    CREATE INDEX IX_SupportTicket_TicketType ON dbo.SupportTicket (TicketType, Status);
GO

IF COL_LENGTH(N'dbo.PatientAppointment', N'BookingChannel') IS NULL
BEGIN
    RAISERROR('SUP-07.01 BookingChannel column missing on PatientAppointment.', 16, 1);
    RETURN;
END

IF OBJECT_ID(N'dbo.SupportTicketType', N'U') IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.SupportTicketType WHERE TicketTypeCode = N'AssistedRequest')
BEGIN
    RAISERROR('SUP-07.01 AssistedRequest ticket type is missing.', 16, 1);
    RETURN;
END
GO

PRINT 'M14_SUP_07_01.sql: BookingChannel=Assisted + AssistedRequest ticket type ready.';

SELECT TicketTypeCode, DisplayName, IsActive FROM dbo.SupportTicketType ORDER BY TicketTypeCode;

SELECT
    COUNT(*) AS AppointmentsWithAssistedChannel
FROM dbo.PatientAppointment
WHERE BookingChannel = N'Assisted';

SELECT TOP 5
    SupportTicketId,
    Category,
    TicketType,
    Status
FROM dbo.SupportTicket
ORDER BY SupportTicketId DESC;
GO

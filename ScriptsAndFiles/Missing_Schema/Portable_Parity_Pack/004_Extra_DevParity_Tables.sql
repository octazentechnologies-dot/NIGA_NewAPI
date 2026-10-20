/*
Purpose: Extra tables present on HomeoCentrum_Dev but not created by S6 or Mobile/01.
Evidence: Dev-only after Mobile+S6 — PatientDeliveryAddress, DoctorCareCategory, PublicBookingIdempotency.
Idempotent. No sample rows. Named PKs (not system-generated names).
Risk: Low (additive).
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PatientDeliveryAddress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientDeliveryAddress (
        DeliveryAddressId int IDENTITY(1,1) NOT NULL,
        PatientId int NOT NULL,
        Label nvarchar(80) NULL,
        Line1 nvarchar(200) NOT NULL,
        Line2 nvarchar(200) NULL,
        City nvarchar(80) NULL,
        State nvarchar(80) NULL,
        Pincode nvarchar(12) NOT NULL,
        Phone nvarchar(30) NULL,
        IsDefault bit NOT NULL CONSTRAINT DF_PatientDeliveryAddress_IsDefault DEFAULT (0),
        CreatedAt datetime NOT NULL,
        CONSTRAINT PK_PatientDeliveryAddress PRIMARY KEY CLUSTERED (DeliveryAddressId)
    );
    PRINT 'Created dbo.PatientDeliveryAddress';
END
ELSE
    PRINT 'dbo.PatientDeliveryAddress already exists';
GO

IF OBJECT_ID(N'dbo.DoctorCareCategory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorCareCategory (
        DoctorId int NOT NULL,
        CareCategoryId int NOT NULL,
        CONSTRAINT PK_DoctorCareCategory PRIMARY KEY CLUSTERED (DoctorId, CareCategoryId)
    );
    PRINT 'Created dbo.DoctorCareCategory';
END
ELSE
    PRINT 'dbo.DoctorCareCategory already exists';
GO

IF OBJECT_ID(N'dbo.PublicBookingIdempotency', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PublicBookingIdempotency (
        PublicBookingIdempotencyId int IDENTITY(1,1) NOT NULL,
        IdempotencyKey nvarchar(80) NOT NULL,
        DoctorId int NOT NULL,
        PatientMobile nvarchar(20) NOT NULL,
        BodyFingerprint nvarchar(200) NOT NULL,
        PatientAppId int NOT NULL,
        BookingToken nvarchar(64) NOT NULL,
        CreatedAt datetime2(7) NOT NULL,
        CONSTRAINT PK_PublicBookingIdempotency PRIMARY KEY CLUSTERED (PublicBookingIdempotencyId)
    );
    PRINT 'Created dbo.PublicBookingIdempotency';
END
ELSE
    PRINT 'dbo.PublicBookingIdempotency already exists';
GO

PRINT '004_Extra_DevParity_Tables completed.';
GO

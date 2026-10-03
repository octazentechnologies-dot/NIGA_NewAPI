/*
    Schema only. Safe to run more than once. Does not insert master rows.
    Run on the other server in this order:
      1. M00_UserAddressLocation_Schema.sql
      2. M00_CountryMaster_FromExcel.sql
      3. M00_StateMaster_FromExcel.sql
      4. M00_IndiaLocation_FromExcel.sql
      5. M00_UserAddressLocation_Other.sql          -- inserts Other only when that row is missing
      6. M00_UserAddressLocation_EnteredBy.sql
      7. M00_UserAddressLocation_UserData.sql
    Country -> State -> District -> City -> PinCode
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH(N'dbo.CountryMaster', N'Iso2Code') IS NULL
    ALTER TABLE dbo.CountryMaster ADD Iso2Code nvarchar(2) NULL;
IF COL_LENGTH(N'dbo.CountryMaster', N'Iso3Code') IS NULL
    ALTER TABLE dbo.CountryMaster ADD Iso3Code nvarchar(3) NULL;
GO

IF OBJECT_ID(N'dbo.DistrictMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DistrictMaster
    (
        DistrictId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DistrictMaster PRIMARY KEY,
        DistrictName nvarchar(100) NOT NULL,
        StateId int NOT NULL,
        EnteredBy nvarchar(50) NULL,
        EnteredDate datetime NULL,
        ChangedBy nvarchar(50) NULL,
        ChangedDate datetime NULL,
        DeleteStatus bit NOT NULL CONSTRAINT DF_DistrictMaster_DeleteStatus DEFAULT (0),
        CONSTRAINT FK_DistrictMaster_StateMaster FOREIGN KEY (StateId) REFERENCES dbo.StateMaster (StateId)
    );
    CREATE UNIQUE INDEX UX_DistrictMaster_State_Name ON dbo.DistrictMaster (StateId, DistrictName);
END
GO

IF OBJECT_ID(N'dbo.CityMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CityMaster
    (
        CityId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CityMaster PRIMARY KEY,
        CityName nvarchar(100) NOT NULL,
        DistrictId int NOT NULL,
        EnteredBy nvarchar(50) NULL,
        EnteredDate datetime NULL,
        ChangedBy nvarchar(50) NULL,
        ChangedDate datetime NULL,
        DeleteStatus bit NOT NULL CONSTRAINT DF_CityMaster_DeleteStatus DEFAULT (0),
        CONSTRAINT FK_CityMaster_DistrictMaster FOREIGN KEY (DistrictId) REFERENCES dbo.DistrictMaster (DistrictId)
    );
    CREATE UNIQUE INDEX UX_CityMaster_District_Name ON dbo.CityMaster (DistrictId, CityName);
END
GO

IF COL_LENGTH(N'dbo.CityMaster', N'DistrictId') IS NULL
    ALTER TABLE dbo.CityMaster ADD DistrictId int NULL;
GO

IF OBJECT_ID(N'dbo.PinCodeMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PinCodeMaster
    (
        PinCodeId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PinCodeMaster PRIMARY KEY,
        PinCode nvarchar(10) NOT NULL,
        CityId int NOT NULL,
        EnteredBy nvarchar(50) NULL,
        EnteredDate datetime NULL,
        ChangedBy nvarchar(50) NULL,
        ChangedDate datetime NULL,
        DeleteStatus bit NOT NULL CONSTRAINT DF_PinCodeMaster_DeleteStatus DEFAULT (0),
        CONSTRAINT FK_PinCodeMaster_CityMaster FOREIGN KEY (CityId) REFERENCES dbo.CityMaster (CityId)
    );
    CREATE UNIQUE INDEX UX_PinCodeMaster_City_Pin ON dbo.PinCodeMaster (CityId, PinCode);
END
GO

IF COL_LENGTH(N'dbo.Doctor', N'DistrictId') IS NULL
    ALTER TABLE dbo.Doctor ADD DistrictId int NULL;
IF COL_LENGTH(N'dbo.Doctor', N'CityId') IS NULL
    ALTER TABLE dbo.Doctor ADD CityId int NULL;
IF COL_LENGTH(N'dbo.Doctor', N'PinCodeId') IS NULL
    ALTER TABLE dbo.Doctor ADD PinCodeId int NULL;

IF COL_LENGTH(N'dbo.Patient', N'DistrictId') IS NULL
    ALTER TABLE dbo.Patient ADD DistrictId int NULL;
IF COL_LENGTH(N'dbo.Patient', N'CityId') IS NULL
    ALTER TABLE dbo.Patient ADD CityId int NULL;
IF COL_LENGTH(N'dbo.Patient', N'PinCodeId') IS NULL
    ALTER TABLE dbo.Patient ADD PinCodeId int NULL;

IF COL_LENGTH(N'dbo.UserMaster', N'DistrictId') IS NULL
    ALTER TABLE dbo.UserMaster ADD DistrictId int NULL;
IF COL_LENGTH(N'dbo.UserMaster', N'CityId') IS NULL
    ALTER TABLE dbo.UserMaster ADD CityId int NULL;
IF COL_LENGTH(N'dbo.UserMaster', N'PinCodeId') IS NULL
    ALTER TABLE dbo.UserMaster ADD PinCodeId int NULL;
GO

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Doctor_DistrictMaster')
    ALTER TABLE dbo.Doctor ADD CONSTRAINT FK_Doctor_DistrictMaster FOREIGN KEY (DistrictId) REFERENCES dbo.DistrictMaster (DistrictId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Doctor_CityMaster')
    ALTER TABLE dbo.Doctor ADD CONSTRAINT FK_Doctor_CityMaster FOREIGN KEY (CityId) REFERENCES dbo.CityMaster (CityId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Doctor_PinCodeMaster')
    ALTER TABLE dbo.Doctor ADD CONSTRAINT FK_Doctor_PinCodeMaster FOREIGN KEY (PinCodeId) REFERENCES dbo.PinCodeMaster (PinCodeId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Patient_DistrictMaster')
    ALTER TABLE dbo.Patient ADD CONSTRAINT FK_Patient_DistrictMaster FOREIGN KEY (DistrictId) REFERENCES dbo.DistrictMaster (DistrictId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Patient_CityMaster')
    ALTER TABLE dbo.Patient ADD CONSTRAINT FK_Patient_CityMaster FOREIGN KEY (CityId) REFERENCES dbo.CityMaster (CityId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Patient_PinCodeMaster')
    ALTER TABLE dbo.Patient ADD CONSTRAINT FK_Patient_PinCodeMaster FOREIGN KEY (PinCodeId) REFERENCES dbo.PinCodeMaster (PinCodeId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserMaster_DistrictMaster')
    ALTER TABLE dbo.UserMaster ADD CONSTRAINT FK_UserMaster_DistrictMaster FOREIGN KEY (DistrictId) REFERENCES dbo.DistrictMaster (DistrictId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserMaster_CityMaster')
    ALTER TABLE dbo.UserMaster ADD CONSTRAINT FK_UserMaster_CityMaster FOREIGN KEY (CityId) REFERENCES dbo.CityMaster (CityId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserMaster_PinCodeMaster')
    ALTER TABLE dbo.UserMaster ADD CONSTRAINT FK_UserMaster_PinCodeMaster FOREIGN KEY (PinCodeId) REFERENCES dbo.PinCodeMaster (PinCodeId);
GO

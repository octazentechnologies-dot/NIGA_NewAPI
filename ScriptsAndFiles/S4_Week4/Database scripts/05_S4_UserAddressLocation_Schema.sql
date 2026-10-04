/*
    Schema only. Safe to run more than once. Does not insert master rows.
    Week 4 folder, after 01 through 04:
      05. 05_S4_UserAddressLocation_Schema.sql
      06. 06_S4_CountryMaster_Data.sql
      07. 07_S4_StateMaster_Data.sql
      08. 08_S4_IndiaLocation_Data.sql
      09. 09_S4_AddressLocation_Other.sql
      10. 10_S4_AddressLocation_EnteredBy.sql
      11. 11_S4_AddressLocation_UserData.sql
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

IF OBJECT_ID(N'dbo.CityMaster', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CityMaster', N'StateId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.CityMaster WHERE DistrictId IS NULL)
BEGIN
    RAISERROR('CityMaster has rows with no DistrictId. Load district links before dropping StateId.', 16, 1);
    RETURN;
END
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CityMaster_StateMaster')
    ALTER TABLE dbo.CityMaster DROP CONSTRAINT FK_CityMaster_StateMaster;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CityMaster_State_Name' AND object_id = OBJECT_ID(N'dbo.CityMaster'))
    DROP INDEX UX_CityMaster_State_Name ON dbo.CityMaster;
GO

IF COL_LENGTH(N'dbo.CityMaster', N'StateId') IS NOT NULL
    ALTER TABLE dbo.CityMaster DROP COLUMN StateId;
GO

IF OBJECT_ID(N'dbo.CityMaster', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.CityMaster', N'DistrictId') IS NOT NULL
   AND EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.CityMaster') AND name = N'DistrictId' AND is_nullable = 1
   )
   AND NOT EXISTS (SELECT 1 FROM dbo.CityMaster WHERE DistrictId IS NULL)
    ALTER TABLE dbo.CityMaster ALTER COLUMN DistrictId int NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CityMaster_DistrictMaster')
    ALTER TABLE dbo.CityMaster ADD CONSTRAINT FK_CityMaster_DistrictMaster
        FOREIGN KEY (DistrictId) REFERENCES dbo.DistrictMaster (DistrictId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CityMaster_District_Name' AND object_id = OBJECT_ID(N'dbo.CityMaster'))
    CREATE UNIQUE INDEX UX_CityMaster_District_Name ON dbo.CityMaster (DistrictId, CityName);
GO

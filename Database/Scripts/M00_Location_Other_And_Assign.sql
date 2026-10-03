/*
    Adds location id columns and foreign keys, then sets Patient, Doctor, and UserMaster.
    Does not insert country, state, district, city, or pin rows. Load those in SSMS.
    India / Maharashtra / Kolhapur / Kolhapur / 416003 must already exist.
    Ids are looked up by name so another server can run this after its own master load.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH(N'dbo.Patient', N'DistrictId') IS NULL
    ALTER TABLE dbo.Patient ADD DistrictId int NULL;
IF COL_LENGTH(N'dbo.Patient', N'CityId') IS NULL
    ALTER TABLE dbo.Patient ADD CityId int NULL;
IF COL_LENGTH(N'dbo.Patient', N'PinCodeId') IS NULL
    ALTER TABLE dbo.Patient ADD PinCodeId int NULL;

IF COL_LENGTH(N'dbo.Doctor', N'PinCodeId') IS NULL
    ALTER TABLE dbo.Doctor ADD PinCodeId int NULL;

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
SET XACT_ABORT ON;
BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Patient_DistrictMaster')
    ALTER TABLE dbo.Patient ADD CONSTRAINT FK_Patient_DistrictMaster FOREIGN KEY (DistrictId) REFERENCES dbo.DistrictMaster (DistrictId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Patient_CityMaster')
    ALTER TABLE dbo.Patient ADD CONSTRAINT FK_Patient_CityMaster FOREIGN KEY (CityId) REFERENCES dbo.CityMaster (CityId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Patient_PinCodeMaster')
    ALTER TABLE dbo.Patient ADD CONSTRAINT FK_Patient_PinCodeMaster FOREIGN KEY (PinCodeId) REFERENCES dbo.PinCodeMaster (PinCodeId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Doctor_PinCodeMaster')
    ALTER TABLE dbo.Doctor ADD CONSTRAINT FK_Doctor_PinCodeMaster FOREIGN KEY (PinCodeId) REFERENCES dbo.PinCodeMaster (PinCodeId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserMaster_DistrictMaster')
    ALTER TABLE dbo.UserMaster ADD CONSTRAINT FK_UserMaster_DistrictMaster FOREIGN KEY (DistrictId) REFERENCES dbo.DistrictMaster (DistrictId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserMaster_CityMaster')
    ALTER TABLE dbo.UserMaster ADD CONSTRAINT FK_UserMaster_CityMaster FOREIGN KEY (CityId) REFERENCES dbo.CityMaster (CityId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserMaster_PinCodeMaster')
    ALTER TABLE dbo.UserMaster ADD CONSTRAINT FK_UserMaster_PinCodeMaster FOREIGN KEY (PinCodeId) REFERENCES dbo.PinCodeMaster (PinCodeId);

DECLARE @CountryId int;
DECLARE @StateId int;
DECLARE @DistrictId int;
DECLARE @CityId int;
DECLARE @PinCodeId int;

SELECT @CountryId = CountryId FROM dbo.CountryMaster WHERE CountryName = N'India' AND ISNULL(DeleteStatus, 0) = 0;
SELECT @StateId = StateId FROM dbo.StateMaster WHERE CountryId = @CountryId AND StateName = N'Maharashtra' AND ISNULL(DeleteStatus, 0) = 0;
SELECT @DistrictId = DistrictId FROM dbo.DistrictMaster WHERE StateId = @StateId AND DistrictName = N'Kolhapur' AND ISNULL(DeleteStatus, 0) = 0;
SELECT @CityId = CityId FROM dbo.CityMaster WHERE DistrictId = @DistrictId AND CityName = N'Kolhapur' AND ISNULL(DeleteStatus, 0) = 0;
SELECT @PinCodeId = PinCodeId FROM dbo.PinCodeMaster WHERE CityId = @CityId AND PinCode = N'416003' AND ISNULL(DeleteStatus, 0) = 0;

IF @CountryId IS NULL OR @StateId IS NULL OR @DistrictId IS NULL OR @CityId IS NULL OR @PinCodeId IS NULL
    THROW 50001, 'India / Maharashtra / Kolhapur / 416003 was not found. Load the master scripts first.', 1;

UPDATE dbo.Patient
SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId;

UPDATE dbo.Doctor
SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId;

UPDATE dbo.UserMaster
SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId;

SELECT @CountryId AS CountryId, @StateId AS StateId, @DistrictId AS DistrictId, @CityId AS CityId, @PinCodeId AS PinCodeId;

COMMIT;
GO

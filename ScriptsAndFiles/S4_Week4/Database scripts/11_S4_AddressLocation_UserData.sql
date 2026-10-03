/*
    Sets every Patient, Doctor, and UserMaster row to India / Maharashtra / Kolhapur / Kolhapur / 416003.
    Looks up the ids by name, so this works on another server after the master scripts.
    On this database those ids are Country 98, State 2818, District 1102, City 107792, Pin 67856.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @CountryId int;
DECLARE @StateId int;
DECLARE @DistrictId int;
DECLARE @CityId int;
DECLARE @PinCodeId int;

SELECT @CountryId = CountryId
FROM dbo.CountryMaster
WHERE CountryName = N'India' AND ISNULL(DeleteStatus, 0) = 0;

SELECT @StateId = StateId
FROM dbo.StateMaster
WHERE CountryId = @CountryId AND StateName = N'Maharashtra' AND ISNULL(DeleteStatus, 0) = 0;

SELECT @DistrictId = DistrictId
FROM dbo.DistrictMaster
WHERE StateId = @StateId AND DistrictName = N'Kolhapur' AND ISNULL(DeleteStatus, 0) = 0;

SELECT @CityId = CityId
FROM dbo.CityMaster
WHERE DistrictId = @DistrictId AND CityName = N'Kolhapur' AND ISNULL(DeleteStatus, 0) = 0;

SELECT @PinCodeId = PinCodeId
FROM dbo.PinCodeMaster
WHERE CityId = @CityId AND PinCode = N'416003' AND ISNULL(DeleteStatus, 0) = 0;

IF @CountryId IS NULL OR @StateId IS NULL OR @DistrictId IS NULL OR @CityId IS NULL OR @PinCodeId IS NULL
    THROW 50001, 'India / Maharashtra / Kolhapur / 416003 was not found. Load the master scripts first.', 1;

BEGIN TRAN;

UPDATE dbo.Patient
SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId;

UPDATE dbo.Doctor
SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId;

UPDATE dbo.UserMaster
SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId;

SELECT @CountryId AS CountryId, @StateId AS StateId, @DistrictId AS DistrictId, @CityId AS CityId, @PinCodeId AS PinCodeId;

COMMIT;
GO

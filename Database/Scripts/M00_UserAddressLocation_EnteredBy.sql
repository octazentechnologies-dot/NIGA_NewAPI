/*
    Address masters: Country, State, District, City, PinCode.
    EnteredBy = Tufan Powar
    EnteredDate = 2 Oct 2026
    DeleteStatus = 0
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @EnteredBy nvarchar(50) = N'Tufan Powar';
DECLARE @EnteredDate datetime = '2026-10-02';

BEGIN TRAN;

UPDATE dbo.CountryMaster
SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0;

UPDATE dbo.StateMaster
SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0;

UPDATE dbo.DistrictMaster
SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0;

UPDATE dbo.CityMaster
SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0;

UPDATE dbo.PinCodeMaster
SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0;

SELECT 'CountryMaster' AS MasterName, COUNT(*) AS Rows
FROM dbo.CountryMaster
WHERE EnteredBy = @EnteredBy AND EnteredDate = @EnteredDate AND DeleteStatus = 0
UNION ALL
SELECT 'StateMaster', COUNT(*)
FROM dbo.StateMaster
WHERE EnteredBy = @EnteredBy AND EnteredDate = @EnteredDate AND DeleteStatus = 0
UNION ALL
SELECT 'DistrictMaster', COUNT(*)
FROM dbo.DistrictMaster
WHERE EnteredBy = @EnteredBy AND EnteredDate = @EnteredDate AND DeleteStatus = 0
UNION ALL
SELECT 'CityMaster', COUNT(*)
FROM dbo.CityMaster
WHERE EnteredBy = @EnteredBy AND EnteredDate = @EnteredDate AND DeleteStatus = 0
UNION ALL
SELECT 'PinCodeMaster', COUNT(*)
FROM dbo.PinCodeMaster
WHERE EnteredBy = @EnteredBy AND EnteredDate = @EnteredDate AND DeleteStatus = 0;

COMMIT;
GO

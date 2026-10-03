/*
    Inserts Other only when that parent does not already have one.
    Safe to run again: existing Other rows are left as they are.
    Run after country, state, district, city, and pin data are loaded.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @EnteredBy nvarchar(50) = N'Tufan Powar';
DECLARE @EnteredDate datetime = '2026-10-02';

BEGIN TRAN;

IF NOT EXISTS (SELECT 1 FROM dbo.CountryMaster WHERE CountryName = N'Other' AND ISNULL(DeleteStatus, 0) = 0)
    INSERT INTO dbo.CountryMaster (CountryName, CountryCode, Iso2Code, Iso3Code, EnteredBy, EnteredDate, DeleteStatus)
    VALUES (N'Other', NULL, NULL, NULL, @EnteredBy, @EnteredDate, 0);

INSERT INTO dbo.StateMaster (StateName, CountryId, EnteredBy, EnteredDate, DeleteStatus)
SELECT N'Other', c.CountryId, @EnteredBy, @EnteredDate, 0
FROM dbo.CountryMaster c
WHERE ISNULL(c.DeleteStatus, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.StateMaster s
      WHERE s.CountryId = c.CountryId AND s.StateName = N'Other' AND ISNULL(s.DeleteStatus, 0) = 0
  );

INSERT INTO dbo.DistrictMaster (DistrictName, StateId, EnteredBy, EnteredDate, DeleteStatus)
SELECT N'Other', s.StateId, @EnteredBy, @EnteredDate, 0
FROM dbo.StateMaster s
WHERE ISNULL(s.DeleteStatus, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.DistrictMaster d
      WHERE d.StateId = s.StateId AND d.DistrictName = N'Other' AND ISNULL(d.DeleteStatus, 0) = 0
  );

INSERT INTO dbo.CityMaster (CityName, DistrictId, EnteredBy, EnteredDate, DeleteStatus)
SELECT N'Other', d.DistrictId, @EnteredBy, @EnteredDate, 0
FROM dbo.DistrictMaster d
WHERE ISNULL(d.DeleteStatus, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.CityMaster c
      WHERE c.DistrictId = d.DistrictId AND c.CityName = N'Other' AND ISNULL(c.DeleteStatus, 0) = 0
  );

INSERT INTO dbo.PinCodeMaster (PinCode, CityId, EnteredBy, EnteredDate, DeleteStatus)
SELECT N'Other', c.CityId, @EnteredBy, @EnteredDate, 0
FROM dbo.CityMaster c
WHERE ISNULL(c.DeleteStatus, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM dbo.PinCodeMaster p
      WHERE p.CityId = c.CityId AND p.PinCode = N'Other' AND ISNULL(p.DeleteStatus, 0) = 0
  );

COMMIT;
GO

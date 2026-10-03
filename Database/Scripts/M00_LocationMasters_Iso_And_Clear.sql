/*
    Adds ISO columns on CountryMaster.
    Does not insert or delete country, state, district, city, or pin rows.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH(N'dbo.CountryMaster', N'Iso2Code') IS NULL
    ALTER TABLE dbo.CountryMaster ADD Iso2Code nvarchar(2) NULL;
IF COL_LENGTH(N'dbo.CountryMaster', N'Iso3Code') IS NULL
    ALTER TABLE dbo.CountryMaster ADD Iso3Code nvarchar(3) NULL;
GO

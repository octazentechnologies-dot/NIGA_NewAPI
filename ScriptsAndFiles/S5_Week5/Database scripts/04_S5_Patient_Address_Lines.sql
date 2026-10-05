/*
================================================================================
Author       : Gourav Nikam
Created      : 05-10-2026
Script       : 04_S5_Patient_Address_Lines.sql
Purpose      : Split patient street address into AddressLine1 / AddressLine2 /
               Landmark for UI booking and patient forms. Keeps legacy Address
               for older clients. Migrates existing Address into AddressLine1.
               Column sizes: AddressLine1 NVARCHAR(MAX), AddressLine2 NVARCHAR(500),
               Landmark NVARCHAR(250). PinCodeId already exists as FK to
               PinCodeMaster (no change).
Use          : HomeoCentrum_Dev / HomeoCentrum_Stage. Idempotent. Manual run.
               Run after 01–03 in this folder:
               1. 01_S5_Week5_Schema.sql
               2. 02_S5_Week5_Menus.sql
               3. 03_S5_Week5_Demo_Data.sql
               4. 04_S5_Patient_Address_Lines.sql
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF COL_LENGTH(N'dbo.Patient', N'AddressLine1') IS NULL
BEGIN
    ALTER TABLE dbo.Patient ADD AddressLine1 NVARCHAR(MAX) NULL;
END
ELSE IF COL_LENGTH(N'dbo.Patient', N'AddressLine1') <> -1
BEGIN
    -- Widen to MAX if an earlier shorter definition exists.
    ALTER TABLE dbo.Patient ALTER COLUMN AddressLine1 NVARCHAR(MAX) NULL;
END
GO

IF COL_LENGTH(N'dbo.Patient', N'AddressLine2') IS NULL
BEGIN
    ALTER TABLE dbo.Patient ADD AddressLine2 NVARCHAR(500) NULL;
END
ELSE IF COL_LENGTH(N'dbo.Patient', N'AddressLine2') IS NOT NULL
   AND COL_LENGTH(N'dbo.Patient', N'AddressLine2') < 1000
BEGIN
    ALTER TABLE dbo.Patient ALTER COLUMN AddressLine2 NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH(N'dbo.Patient', N'Landmark') IS NULL
BEGIN
    ALTER TABLE dbo.Patient ADD Landmark NVARCHAR(250) NULL;
END
ELSE IF COL_LENGTH(N'dbo.Patient', N'Landmark') IS NOT NULL
   AND COL_LENGTH(N'dbo.Patient', N'Landmark') < 500
BEGIN
    ALTER TABLE dbo.Patient ALTER COLUMN Landmark NVARCHAR(250) NULL;
END
GO

-- One-time migrate: copy legacy Address into AddressLine1 when blank.
IF COL_LENGTH(N'dbo.Patient', N'AddressLine1') IS NOT NULL
   AND COL_LENGTH(N'dbo.Patient', N'Address') IS NOT NULL
BEGIN
    UPDATE dbo.Patient
    SET AddressLine1 = LTRIM(RTRIM(Address))
    WHERE AddressLine1 IS NULL
      AND Address IS NOT NULL
      AND LTRIM(RTRIM(Address)) <> N'';
END
GO

SELECT
    COL_LENGTH(N'dbo.Patient', N'AddressLine1') AS AddressLine1Len,
    COL_LENGTH(N'dbo.Patient', N'AddressLine2') AS AddressLine2Len,
    COL_LENGTH(N'dbo.Patient', N'Landmark') AS LandmarkLen,
    COL_LENGTH(N'dbo.Patient', N'PinCodeId') AS PinCodeIdLen,
    (SELECT COUNT(*) FROM dbo.Patient WHERE AddressLine1 IS NOT NULL AND LTRIM(RTRIM(AddressLine1)) <> N'') AS RowsWithAddressLine1;
GO

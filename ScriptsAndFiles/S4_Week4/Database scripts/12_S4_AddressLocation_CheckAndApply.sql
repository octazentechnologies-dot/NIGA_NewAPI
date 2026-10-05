/*
    Run this on the other database when some address scripts were already executed.
    It adds a column, table, key, or index only when that object is missing.
    It inserts Other only when that parent does not already have one.
    It sets EnteredBy only on address rows where EnteredBy is still empty.
    It sets Patient, Doctor, and UserMaster location ids only on rows where DistrictId is still null.
    It does not reload country, state, or India city rows.
    After it finishes, the last result tells you whether 06, 07, or 08 still need to be run.
    Those three files skip a name that already exists. 06 also fills Iso2Code and Iso3Code only when the code is null.
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

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

DECLARE @EnteredBy nvarchar(50) = N'Tufan Powar';
DECLARE @EnteredDate datetime = '2026-10-02';

IF OBJECT_ID(N'dbo.CountryMaster', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.CountryMaster WHERE CountryName = N'Other' AND ISNULL(DeleteStatus, 0) = 0)
    INSERT INTO dbo.CountryMaster (CountryName, CountryCode, Iso2Code, Iso3Code, EnteredBy, EnteredDate, DeleteStatus)
    VALUES (N'Other', NULL, NULL, NULL, @EnteredBy, @EnteredDate, 0);

IF OBJECT_ID(N'dbo.StateMaster', N'U') IS NOT NULL
    INSERT INTO dbo.StateMaster (StateName, CountryId, EnteredBy, EnteredDate, DeleteStatus)
    SELECT N'Other', c.CountryId, @EnteredBy, @EnteredDate, 0
    FROM dbo.CountryMaster c
    WHERE ISNULL(c.DeleteStatus, 0) = 0
      AND NOT EXISTS (
          SELECT 1 FROM dbo.StateMaster s
          WHERE s.CountryId = c.CountryId AND s.StateName = N'Other' AND ISNULL(s.DeleteStatus, 0) = 0
      );

IF OBJECT_ID(N'dbo.DistrictMaster', N'U') IS NOT NULL
    INSERT INTO dbo.DistrictMaster (DistrictName, StateId, EnteredBy, EnteredDate, DeleteStatus)
    SELECT N'Other', s.StateId, @EnteredBy, @EnteredDate, 0
    FROM dbo.StateMaster s
    WHERE ISNULL(s.DeleteStatus, 0) = 0
      AND NOT EXISTS (
          SELECT 1 FROM dbo.DistrictMaster d
          WHERE d.StateId = s.StateId AND d.DistrictName = N'Other' AND ISNULL(d.DeleteStatus, 0) = 0
      );

IF OBJECT_ID(N'dbo.CityMaster', N'U') IS NOT NULL
    INSERT INTO dbo.CityMaster (CityName, DistrictId, EnteredBy, EnteredDate, DeleteStatus)
    SELECT N'Other', d.DistrictId, @EnteredBy, @EnteredDate, 0
    FROM dbo.DistrictMaster d
    WHERE ISNULL(d.DeleteStatus, 0) = 0
      AND NOT EXISTS (
          SELECT 1 FROM dbo.CityMaster c
          WHERE c.DistrictId = d.DistrictId AND c.CityName = N'Other' AND ISNULL(c.DeleteStatus, 0) = 0
      );

IF OBJECT_ID(N'dbo.PinCodeMaster', N'U') IS NOT NULL
    INSERT INTO dbo.PinCodeMaster (PinCode, CityId, EnteredBy, EnteredDate, DeleteStatus)
    SELECT N'Other', c.CityId, @EnteredBy, @EnteredDate, 0
    FROM dbo.CityMaster c
    WHERE ISNULL(c.DeleteStatus, 0) = 0
      AND NOT EXISTS (
          SELECT 1 FROM dbo.PinCodeMaster p
          WHERE p.CityId = c.CityId AND p.PinCode = N'Other' AND ISNULL(p.DeleteStatus, 0) = 0
      );

IF COL_LENGTH(N'dbo.CountryMaster', N'EnteredBy') IS NOT NULL
    UPDATE dbo.CountryMaster
    SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0
    WHERE EnteredBy IS NULL OR LTRIM(RTRIM(EnteredBy)) = N'';
IF COL_LENGTH(N'dbo.StateMaster', N'EnteredBy') IS NOT NULL
    UPDATE dbo.StateMaster
    SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0
    WHERE EnteredBy IS NULL OR LTRIM(RTRIM(EnteredBy)) = N'';
IF COL_LENGTH(N'dbo.DistrictMaster', N'EnteredBy') IS NOT NULL
    UPDATE dbo.DistrictMaster
    SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0
    WHERE EnteredBy IS NULL OR LTRIM(RTRIM(EnteredBy)) = N'';
IF COL_LENGTH(N'dbo.CityMaster', N'EnteredBy') IS NOT NULL
    UPDATE dbo.CityMaster
    SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0
    WHERE EnteredBy IS NULL OR LTRIM(RTRIM(EnteredBy)) = N'';
IF COL_LENGTH(N'dbo.PinCodeMaster', N'EnteredBy') IS NOT NULL
    UPDATE dbo.PinCodeMaster
    SET EnteredBy = @EnteredBy, EnteredDate = @EnteredDate, DeleteStatus = 0
    WHERE EnteredBy IS NULL OR LTRIM(RTRIM(EnteredBy)) = N'';

DECLARE @CountryId int, @StateId int, @DistrictId int, @CityId int, @PinCodeId int;
IF OBJECT_ID(N'dbo.CountryMaster', N'U') IS NOT NULL
    SELECT @CountryId = CountryId FROM dbo.CountryMaster WHERE CountryName = N'India' AND ISNULL(DeleteStatus, 0) = 0;
IF OBJECT_ID(N'dbo.StateMaster', N'U') IS NOT NULL AND @CountryId IS NOT NULL
    SELECT @StateId = StateId FROM dbo.StateMaster WHERE CountryId = @CountryId AND StateName = N'Maharashtra' AND ISNULL(DeleteStatus, 0) = 0;
IF OBJECT_ID(N'dbo.DistrictMaster', N'U') IS NOT NULL AND @StateId IS NOT NULL
    SELECT @DistrictId = DistrictId FROM dbo.DistrictMaster WHERE StateId = @StateId AND DistrictName = N'Kolhapur' AND ISNULL(DeleteStatus, 0) = 0;
IF OBJECT_ID(N'dbo.CityMaster', N'U') IS NOT NULL AND @DistrictId IS NOT NULL
    SELECT @CityId = CityId FROM dbo.CityMaster WHERE DistrictId = @DistrictId AND CityName = N'Kolhapur' AND ISNULL(DeleteStatus, 0) = 0;
IF OBJECT_ID(N'dbo.PinCodeMaster', N'U') IS NOT NULL AND @CityId IS NOT NULL
    SELECT @PinCodeId = PinCodeId FROM dbo.PinCodeMaster WHERE CityId = @CityId AND PinCode = N'416003' AND ISNULL(DeleteStatus, 0) = 0;

IF @CountryId IS NOT NULL AND @StateId IS NOT NULL AND @DistrictId IS NOT NULL AND @CityId IS NOT NULL AND @PinCodeId IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.Patient', N'DistrictId') IS NOT NULL
        UPDATE dbo.Patient
        SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId
        WHERE DistrictId IS NULL;
    IF COL_LENGTH(N'dbo.Doctor', N'DistrictId') IS NOT NULL
        UPDATE dbo.Doctor
        SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId
        WHERE DistrictId IS NULL;
    IF COL_LENGTH(N'dbo.UserMaster', N'DistrictId') IS NOT NULL
        UPDATE dbo.UserMaster
        SET CountryId = @CountryId, StateId = @StateId, DistrictId = @DistrictId, CityId = @CityId, PinCodeId = @PinCodeId
        WHERE DistrictId IS NULL;
END

SELECT
    CASE WHEN COL_LENGTH(N'dbo.CountryMaster', N'Iso2Code') IS NULL THEN N'Missing column' ELSE N'Present' END AS Iso2Code,
    CASE WHEN COL_LENGTH(N'dbo.CountryMaster', N'Iso3Code') IS NULL THEN N'Missing column' ELSE N'Present' END AS Iso3Code,
    CASE WHEN OBJECT_ID(N'dbo.DistrictMaster', N'U') IS NULL THEN N'Missing table' ELSE N'Present' END AS DistrictMaster,
    CASE WHEN OBJECT_ID(N'dbo.CityMaster', N'U') IS NULL THEN N'Missing table' ELSE N'Present' END AS CityMaster,
    CASE WHEN OBJECT_ID(N'dbo.PinCodeMaster', N'U') IS NULL THEN N'Missing table' ELSE N'Present' END AS PinCodeMaster,
    (SELECT COUNT(*) FROM dbo.CountryMaster WHERE ISNULL(DeleteStatus, 0) = 0) AS Countries,
    (SELECT COUNT(*) FROM dbo.CountryMaster WHERE ISNULL(DeleteStatus, 0) = 0 AND (Iso2Code IS NULL OR Iso3Code IS NULL) AND CountryName <> N'Other') AS CountriesMissingIso,
    CASE WHEN (SELECT COUNT(*) FROM dbo.CountryMaster WHERE ISNULL(DeleteStatus, 0) = 0 AND CountryName <> N'Other') < 200
         THEN N'Run 06_S4_CountryMaster_Data.sql' ELSE N'Countries loaded' END AS CountryData,
    CASE WHEN OBJECT_ID(N'dbo.StateMaster', N'U') IS NULL THEN N'Run 07 after countries'
         WHEN (SELECT COUNT(*) FROM dbo.StateMaster) < 1000 THEN N'Run 07_S4_StateMaster_Data.sql'
         ELSE N'States loaded' END AS StateData,
    CASE WHEN OBJECT_ID(N'dbo.DistrictMaster', N'U') IS NULL THEN N'District table missing'
         WHEN (SELECT COUNT(*) FROM dbo.DistrictMaster WHERE DistrictName <> N'Other') < 100 THEN N'Run 08_S4_IndiaLocation_Data.sql'
         ELSE N'India districts loaded' END AS IndiaData,
    @CountryId AS IndiaCountryId,
    @StateId AS MaharashtraStateId,
    @DistrictId AS KolhapurDistrictId,
    @CityId AS KolhapurCityId,
    @PinCodeId AS Pin416003Id;
GO

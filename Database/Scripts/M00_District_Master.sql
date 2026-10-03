/*
    Schema only. Does not insert country, state, district, city, or pin rows.
    Load those rows in SSMS.
    Country -> State -> District -> City -> PinCode
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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

IF COL_LENGTH(N'dbo.CityMaster', N'DistrictId') IS NULL
    ALTER TABLE dbo.CityMaster ADD DistrictId int NULL;
GO

IF OBJECT_ID(N'dbo.CityMaster', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM dbo.CityMaster WHERE DistrictId IS NULL)
BEGIN
    RAISERROR('CityMaster has rows with no DistrictId. Load district links in SSMS before dropping StateId.', 16, 1);
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
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.CityMaster')
          AND name = N'DistrictId'
          AND is_nullable = 1
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

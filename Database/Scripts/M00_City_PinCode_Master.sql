/*
    Schema only. Does not insert city or pin rows.
    City parent is District. Pin parent is City.
    Load city and pin rows in SSMS.
*/
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

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

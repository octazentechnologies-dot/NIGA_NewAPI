/*
    CityMaster parent = StateMaster
    PinCodeMaster parent = CityMaster
    Country -> State -> City -> PinCode
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CityMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CityMaster
    (
        CityId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CityMaster PRIMARY KEY,
        CityName nvarchar(100) NOT NULL,
        StateId int NOT NULL,
        EnteredBy nvarchar(50) NULL,
        EnteredDate datetime NULL,
        ChangedBy nvarchar(50) NULL,
        ChangedDate datetime NULL,
        DeleteStatus bit NOT NULL CONSTRAINT DF_CityMaster_DeleteStatus DEFAULT (0),
        CONSTRAINT FK_CityMaster_StateMaster FOREIGN KEY (StateId) REFERENCES dbo.StateMaster (StateId)
    );
    CREATE UNIQUE INDEX UX_CityMaster_State_Name ON dbo.CityMaster (StateId, CityName);
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

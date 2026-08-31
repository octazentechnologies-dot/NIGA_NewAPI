-- Internal gold standard benchmark cases (250+ target)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'GoldCaseLibrary' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.GoldCaseLibrary
    (
        GoldCaseId              BIGINT IDENTITY(1,1) NOT NULL,
        Category                NVARCHAR(50) NOT NULL,
        Transcript              NVARCHAR(MAX) NOT NULL,
        SourceLanguage          NVARCHAR(10) NULL,
        DoctorRubricsJson       NVARCHAR(MAX) NOT NULL,
        PrimaryRubricIdsJson    NVARCHAR(MAX) NOT NULL,
        FinalRemedy             NVARCHAR(200) NULL,
        FollowUpOutcome         NVARCHAR(1000) NULL,
        ReviewedBy              INT NULL,
        ReviewDate              DATETIME NULL,
        IsActive                BIT NOT NULL CONSTRAINT DF_GoldCaseLibrary_IsActive DEFAULT (1),
        EnteredDate             DATETIME NOT NULL CONSTRAINT DF_GoldCaseLibrary_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_GoldCaseLibrary PRIMARY KEY (GoldCaseId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_GoldCaseLibrary_Category
    ON dbo.GoldCaseLibrary (Category)
    WHERE IsActive = 1;
GO

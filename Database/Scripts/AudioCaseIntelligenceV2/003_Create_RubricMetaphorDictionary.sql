-- Patient expression → clinical/rubric meaning mappings

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricMetaphorDictionary' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricMetaphorDictionary
    (
        MetaphorId           BIGINT IDENTITY(1,1) NOT NULL,
        PatientExpression    NVARCHAR(500) NOT NULL,
        NormalizedExpression NVARCHAR(500) NOT NULL,
        ClinicalMeaning      NVARCHAR(1000) NOT NULL,
        RubricMeaning        NVARCHAR(500) NOT NULL,
        SubSectionId         INT NULL,
        Language             NVARCHAR(10) NOT NULL,
        ConfidenceWeight     DECIMAL(5,4) NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_ConfidenceWeight DEFAULT (0.85),
        ApprovalStatus       NVARCHAR(20) NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_ApprovalStatus DEFAULT ('Pending'),
        UsageCount           INT NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_UsageCount DEFAULT (0),
        AcceptanceRate       DECIMAL(5,4) NULL,
        VersionNo            INT NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_VersionNo DEFAULT (1),
        IsActive             BIT NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_IsActive DEFAULT (1),
        EnteredBy            INT NULL,
        EnteredDate          DATETIME NOT NULL CONSTRAINT DF_RubricMetaphorDictionary_EnteredDate DEFAULT (GETUTCDATE()),
        ApprovedBy           INT NULL,
        ApprovedDate         DATETIME NULL,
        CONSTRAINT PK_RubricMetaphorDictionary PRIMARY KEY (MetaphorId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_RubricMetaphorDictionary_Normalized
    ON dbo.RubricMetaphorDictionary (NormalizedExpression, Language)
    WHERE IsActive = 1;
GO

-- Configurable homeopathic hierarchy weights for rubric scoring

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'HomeopathicWeightRule' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.HomeopathicWeightRule
    (
        WeightRuleId    INT IDENTITY(1,1) NOT NULL,
        RuleCode        NVARCHAR(50) NOT NULL,
        Category        NVARCHAR(50) NOT NULL,
        WeightValue     DECIMAL(5,2) NOT NULL,
        Description     NVARCHAR(500) NULL,
        IsActive        BIT NOT NULL CONSTRAINT DF_HomeopathicWeightRule_IsActive DEFAULT (1),
        EnteredDate     DATETIME NOT NULL CONSTRAINT DF_HomeopathicWeightRule_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_HomeopathicWeightRule PRIMARY KEY (WeightRuleId),
        CONSTRAINT UQ_HomeopathicWeightRule_RuleCode UNIQUE (RuleCode)
    );
END
GO

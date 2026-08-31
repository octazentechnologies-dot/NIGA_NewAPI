IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AIRolloutGate' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AIRolloutGate
    (
        RolloutGateId BIGINT IDENTITY(1,1) NOT NULL,
        FlagName NVARCHAR(100) NOT NULL,
        BenchmarkRunUtc DATETIME2 NOT NULL,
        Top5Accuracy DECIMAL(5,4) NULL,
        DoctorAcceptanceRate DECIMAL(5,4) NULL,
        ApprovedByUserId INT NOT NULL,
        Notes NVARCHAR(1000) NULL,
        EnteredDate DATETIME2 NOT NULL CONSTRAINT DF_AIRolloutGate_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIRolloutGate PRIMARY KEY (RolloutGateId)
    );
    CREATE INDEX IX_AIRolloutGate_FlagName_BenchmarkRunUtc
        ON dbo.AIRolloutGate (FlagName, BenchmarkRunUtc DESC);
END
GO

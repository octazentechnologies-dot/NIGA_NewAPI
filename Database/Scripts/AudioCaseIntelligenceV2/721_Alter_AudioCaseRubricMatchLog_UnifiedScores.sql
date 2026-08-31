-- Task 6: unified scoring columns on AudioCaseRubricMatchLog (additive)

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'UnifiedSource') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD UnifiedSource NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'FinalHybridScore') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD FinalHybridScore DECIMAL(5,4) NULL;
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'EvidenceChainComplete') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD EvidenceChainComplete BIT NULL;
END
GO

IF COL_LENGTH('dbo.AudioCaseRubricMatchLog', 'GroundedInOntology') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseRubricMatchLog ADD GroundedInOntology BIT NULL;
END
GO

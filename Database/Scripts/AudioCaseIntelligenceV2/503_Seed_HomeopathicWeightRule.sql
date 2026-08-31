-- Default homeopathic weight hierarchy
-- Run AFTER 006_Create_HomeopathicWeightRule.sql

MERGE dbo.HomeopathicWeightRule AS target
USING (VALUES
    (N'SRP',              N'srp',          10.00, N'Strange, rare, peculiar symptoms'),
    (N'MENTAL',           N'mental',        8.00, N'Mental/emotional symptoms'),
    (N'CAUSATION',        N'causation',     7.00, N'Clear causation chain'),
    (N'GENERAL',          N'general',       5.00, N'General symptoms'),
    (N'PARTICULAR',       N'particular',    4.00, N'Particular/local symptoms'),
    (N'CONFIRMATORY',     N'confirmatory',  2.00, N'Confirmatory rubrics'),
    (N'CONCOMITANT',      N'concomitant',   3.50, N'Concomitant symptoms')
) AS source (RuleCode, Category, WeightValue, Description)
ON target.RuleCode = source.RuleCode
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RuleCode, Category, WeightValue, Description, IsActive)
    VALUES (source.RuleCode, source.Category, source.WeightValue, source.Description, 1);
GO

PRINT 'Homeopathic weight rules seeded.';
GO

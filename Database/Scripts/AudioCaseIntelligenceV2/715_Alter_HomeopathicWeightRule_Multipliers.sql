IF COL_LENGTH('dbo.HomeopathicWeightRule', 'MultiplierValue') IS NULL
    ALTER TABLE dbo.HomeopathicWeightRule ADD MultiplierValue DECIMAL(6,3) NULL;
GO

IF COL_LENGTH('dbo.HomeopathicWeightRule', 'SetByUserId') IS NULL
    ALTER TABLE dbo.HomeopathicWeightRule ADD SetByUserId INT NULL;
GO

IF COL_LENGTH('dbo.HomeopathicWeightRule', 'Notes') IS NULL
    ALTER TABLE dbo.HomeopathicWeightRule ADD Notes NVARCHAR(1000) NULL;
GO

MERGE dbo.HomeopathicWeightRule AS target
USING (VALUES
    (N'CausationLinked', N'causation',       CAST(7.00 AS DECIMAL(5,2)), CAST(1.400 AS DECIMAL(6,3)), N'Causation-linked symptom weighting'),
    (N'SRP',             N'srp',             CAST(10.00 AS DECIMAL(5,2)), CAST(1.500 AS DECIMAL(6,3)), N'Strange, rare, peculiar symptom weighting'),
    (N'MentalGeneral',   N'mental',          CAST(8.00 AS DECIMAL(5,2)), CAST(1.200 AS DECIMAL(6,3)), N'Mental general weighting'),
    (N'PhysicalGeneral', N'general',         CAST(5.00 AS DECIMAL(5,2)), CAST(1.100 AS DECIMAL(6,3)), N'Physical general weighting'),
    (N'Modality',        N'modality',        CAST(5.00 AS DECIMAL(5,2)), CAST(1.100 AS DECIMAL(6,3)), N'Modality weighting'),
    (N'Concomitant',     N'concomitant',     CAST(3.50 AS DECIMAL(5,2)), CAST(1.050 AS DECIMAL(6,3)), N'Concomitant weighting'),
    (N'Particular',      N'particular',      CAST(4.00 AS DECIMAL(5,2)), CAST(1.000 AS DECIMAL(6,3)), N'Particular symptom weighting')
) AS source (RuleCode, Category, WeightValue, MultiplierValue, Description)
ON target.RuleCode = source.RuleCode
WHEN MATCHED THEN
    UPDATE SET target.Category = source.Category,
               target.WeightValue = source.WeightValue,
               target.MultiplierValue = source.MultiplierValue,
               target.Description = source.Description
WHEN NOT MATCHED BY TARGET THEN
    INSERT (RuleCode, Category, WeightValue, MultiplierValue, Description, IsActive)
    VALUES (source.RuleCode, source.Category, source.WeightValue, source.MultiplierValue, source.Description, 1);
GO

UPDATE dbo.HomeopathicWeightRule
SET MultiplierValue = CASE
    WHEN Category = N'causation' THEN CAST(1.400 AS DECIMAL(6,3))
    WHEN Category = N'srp' THEN CAST(1.500 AS DECIMAL(6,3))
    ELSE MultiplierValue
END
WHERE Category IN (N'causation', N'srp');
GO

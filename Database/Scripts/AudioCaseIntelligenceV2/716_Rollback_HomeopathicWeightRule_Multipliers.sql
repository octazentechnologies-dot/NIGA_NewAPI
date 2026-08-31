IF COL_LENGTH('dbo.HomeopathicWeightRule', 'Notes') IS NOT NULL
    ALTER TABLE dbo.HomeopathicWeightRule DROP COLUMN Notes;
GO

IF COL_LENGTH('dbo.HomeopathicWeightRule', 'SetByUserId') IS NOT NULL
    ALTER TABLE dbo.HomeopathicWeightRule DROP COLUMN SetByUserId;
GO

IF COL_LENGTH('dbo.HomeopathicWeightRule', 'MultiplierValue') IS NOT NULL
    ALTER TABLE dbo.HomeopathicWeightRule DROP COLUMN MultiplierValue;
GO

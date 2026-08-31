/*
  707 — Bootstrap aliases for GPT/clinical concept names → repertory patterns.
  Run on production after 000_DEPLOY_V3_5_ALL.sql if rubrics were empty despite good concepts.
*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AIConceptMappingBootstrap', N'U') IS NULL
BEGIN
    RAISERROR('AIConceptMappingBootstrap table missing. Run 000_DEPLOY_V3_ALL.sql first.', 16, 1);
    RETURN;
END
GO

;WITH Seed707 AS (
    SELECT * FROM (VALUES
        (N'Prodromal Sensation',            N'%SHOCK%SENSAT%',             N'General',     1),
        (N'Prodromal Sensation',            N'%VIBRAT%',                   N'General',     2),
        (N'Prodromal Sensation',            N'%CONVULS%AURA%',             N'Neurology',   1),
        (N'Shaking Sensation',              N'%SHOCK%SENSAT%',             N'General',     1),
        (N'Shaking Sensation',              N'%TREMBL%CHEST%',             N'Chest',       1),
        (N'Desire for Sweets',              N'%DESIRE%SWEET%',             N'Food',        1),
        (N'Desire for Sweets',              N'%SWEET%DESIRE%',             N'Food',        2),
        (N'Desire For Sweets',              N'%DESIRE%SWEET%',             N'Food',        1),
        (N'Talking in Sleep',               N'%SLEEP%TALK%',               N'Sleep',       1),
        (N'Talking In Sleep',               N'%SLEEP%TALK%',               N'Sleep',       1),
        (N'Fear from Emotional States',     N'%FEAR%IRRIT%',               N'Mind',        1),
        (N'Fear from Emotional States',     N'%FEAR%ANGER%',               N'Mind',        2),
        (N'Fear from Emotional States',     N'%AILMENT%ANGER%',            N'Mind',        3),
        (N'Fear When Sad',                  N'%FEAR%GRIEF%',               N'Mind',        1),
        (N'Fear When Angry',                N'%FEAR%ANGER%',               N'Mind',        1),
        (N'Anticipatory Anxiety',           N'%FEAR%FIT%',                 N'Mind',        3),
        (N'Anticipatory Anxiety',           N'%FEAR%EPILEP%',              N'Neurology',   1),
        (N'Fear of the Fit',                N'%FEAR%CONVULS%',             N'Mind',        1),
        (N'Fear of the Fit',                N'%FEAR%FIT%',                 N'Mind',        2),
        (N'Forget Things',                  N'%FORGET%',                   N'Mind',        1),
        (N'Drops Things',                   N'%DROP%THING%',               N'Extremities', 1),
        (N'Smell of Sourness',              N'%ODOR%SOUR%',                N'Nose',        1),
        (N'Smell When Angry',               N'%ODOR%ANGER%',               N'Nose',        1),
        (N'Sexual Desire',                  N'%SEXUAL%DESIRE%',            N'Sexual',      1),
        (N'Sexual Desire Suppressed',       N'%SEXUAL%DESIRE%SUPPRES%',    N'Sexual',      1)
    ) AS v(HomeopathicConceptPattern, SubSectionNamePattern, Domain, PriorityOrder)
)
INSERT INTO dbo.AIConceptMappingBootstrap
    (HomeopathicConceptPattern, SubSectionNamePattern, Domain, PriorityOrder, IsActive)
SELECT s.HomeopathicConceptPattern, s.SubSectionNamePattern, s.Domain, s.PriorityOrder, 1
FROM Seed707 s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.AIConceptMappingBootstrap b
    WHERE b.HomeopathicConceptPattern = s.HomeopathicConceptPattern
      AND b.SubSectionNamePattern = s.SubSectionNamePattern
);
GO

PRINT '707 bootstrap concept alias patterns seeded.';
GO

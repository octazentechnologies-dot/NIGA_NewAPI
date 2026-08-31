-- =============================================================================
-- V3 Bootstrap: Common case types (Anxiety, Fever, GI, Headache, Sleep, etc.)
-- Run AFTER 000_DEPLOY_V3_ALL.sql (or 703_Seed_AIConceptGraph_Bootstrap.sql)
-- Idempotent — skips rows that already exist (same concept + pattern pair).
-- Does NOT modify SectionMaster or SubSectionMaster.
-- =============================================================================

SET NOCOUNT ON;
PRINT '=== 705 Common-case bootstrap patterns START ===';
GO

IF OBJECT_ID(N'dbo.AIConceptMappingBootstrap', N'U') IS NULL
BEGIN
    RAISERROR('AIConceptMappingBootstrap table not found. Run 000_DEPLOY_V3_ALL.sql first.', 16, 1);
    RETURN;
END
GO

;WITH SeedRows AS (
    SELECT * FROM (VALUES
        -- MIND / ANXIETY / FEAR
        (N'Anxiety',                    N'%ANXIET%',                   N'Mind',      1),
        (N'Anxiety',                    N'%RESTLESS%ANXIET%',          N'Mind',      2),
        (N'Generalized Anxiety',        N'%ANXIET%',                   N'Mind',      1),
        (N'Panic Attack',               N'%PANIC%',                    N'Mind',      1),
        (N'Panic Attack',               N'%FEAR%SUDDEN%',              N'Mind',      2),
        (N'Fear',                       N'%FEAR%',                     N'Mind',      1),
        (N'Anticipatory Anxiety',       N'%FEAR%HAPPEN%',              N'Mind',      1),
        (N'Anticipatory Anxiety',       N'%FEAR%FUTURE%',              N'Mind',      2),
        (N'Restlessness',               N'%RESTLESS%',                 N'Mind',      1),
        (N'Restlessness',               N'%CANNOT%SIT%',               N'Mind',      2),
        (N'Irritability',               N'%IRRITAB%',                  N'Mind',      1),
        (N'Anger',                      N'%ANGER%',                    N'Mind',      1),
        (N'Grief',                      N'%GRIEF%',                    N'Mind',      1),
        (N'Grief',                      N'%SORROW%',                   N'Mind',      2),
        (N'Depression',                 N'%DEPRESS%',                  N'Mind',      1),
        (N'Depression',                 N'%MELANCH%',                  N'Mind',      2),
        (N'Weeping',                    N'%WEEP%',                     N'Mind',      1),
        (N'Fear of Death',              N'%FEAR%DEATH%',               N'Mind',      1),
        (N'Fear of Darkness',           N'%FEAR%DARK%',                N'Mind',      1),
        (N'Obsessive Thoughts',         N'%OBSESS%',                   N'Mind',      1),
        (N'Obsessive Thoughts',         N'%FIXED%IDEA%',               N'Mind',      2),

        -- FEVER / INFLAMMATION
        (N'Fever',                      N'%FEVER%',                    N'Fever',     1),
        (N'High Fever',                 N'%FEVER%HIGH%',               N'Fever',     1),
        (N'High Fever',                 N'%FEVER%INTENSE%',            N'Fever',     2),
        (N'Chill',                      N'%CHILL%',                    N'Fever',     1),
        (N'Chill with Fever',           N'%CHILL%FEVER%',              N'Fever',     1),
        (N'Sweat',                      N'%PERSPIR%',                  N'Fever',     1),
        (N'Night Sweats',               N'%SWEAT%NIGHT%',              N'Fever',     1),
        (N'Burning Heat',               N'%HEAT%BURN%',                N'Fever',     1),
        (N'Burning Heat',               N'%BURNING%HEAT%',             N'Fever',     2),

        -- HEAD / NEURO
        (N'Headache',                   N'%HEAD%ACHE%',                N'Head',      1),
        (N'Headache',                   N'%PAIN%HEAD%',                N'Head',      2),
        (N'Migraine',                   N'%MIGRAIN%',                  N'Head',      1),
        (N'Bursting Headache',          N'%BURST%HEAD%',               N'Head',      1),
        (N'Throbbing Headache',         N'%THROB%HEAD%',               N'Head',      1),
        (N'Vertigo',                    N'%VERTIGO%',                  N'Head',      1),
        (N'Dizziness',                  N'%DIZZ%',                     N'Head',      1),
        (N'Dizziness',                  N'%GIDDY%',                    N'Head',      2),

        -- SLEEP
        (N'Insomnia',                   N'%SLEEP%LESS%',               N'Sleep',     1),
        (N'Insomnia',                   N'%INSOMN%',                   N'Sleep',     2),
        (N'Sleeplessness',              N'%SLEEP%LESS%',               N'Sleep',     1),
        (N'Difficulty Falling Asleep',  N'%SLEEP%DIFFIC%',             N'Sleep',     1),
        (N'Nightmares',                 N'%NIGHTMARE%',                N'Sleep',     1),
        (N'Drowsiness',                 N'%DROWS%',                    N'Sleep',     1),
        (N'Drowsiness',                 N'%SOMNOL%',                   N'Sleep',     2),

        -- GI — STOMACH / ABDOMEN
        (N'Nausea',                     N'%NAUSEA%',                   N'GI',        1),
        (N'Vomiting',                   N'%VOMIT%',                    N'GI',        1),
        (N'Nausea and Vomiting',        N'%NAUSEA%VOMIT%',             N'GI',        1),
        (N'Acidity',                    N'%ACID%',                     N'GI',        1),
        (N'Acidity',                    N'%HEARTBURN%',                N'GI',        2),
        (N'Heartburn',                  N'%HEARTBURN%',                N'GI',        1),
        (N'Indigestion',                N'%INDIGEST%',                 N'GI',        1),
        (N'Bloating',                   N'%BLOAT%',                    N'GI',        1),
        (N'Bloating',                   N'%DISTEN%ABDOM%',             N'GI',        2),
        (N'Abdominal Pain',             N'%PAIN%ABDOM%',               N'GI',        1),
        (N'Abdominal Pain',             N'%COLIC%',                    N'GI',        2),
        (N'Colic',                      N'%COLIC%',                    N'GI',        1),
        (N'Diarrhea',                   N'%DIARR%',                    N'GI',        1),
        (N'Loose Stools',               N'%STOOL%LOOSE%',              N'GI',        1),
        (N'Constipation',               N'%CONSTIP%',                  N'GI',        1),
        (N'Constipation',               N'%STOOL%DIFFIC%',             N'GI',        2),
        (N'Flatulence',                 N'%FLATUL%',                   N'GI',        1),
        (N'Appetite Loss',              N'%APPETITE%LOSS%',            N'GI',        1),
        (N'Appetite Loss',              N'%APPETITE%DIMIN%',           N'GI',        2),
        (N'Increased Appetite',         N'%APPETITE%INCREAS%',         N'GI',        1),
        (N'Thirst',                     N'%THIRST%',                   N'GI',        1),
        (N'Thirstlessness',             N'%THIRST%LESS%',              N'GI',        1),

        -- RESPIRATORY
        (N'Cough',                      N'%COUGH%',                    N'Chest',     1),
        (N'Dry Cough',                  N'%COUGH%DRY%',                N'Chest',     1),
        (N'Loose Cough',                N'%COUGH%LOOSE%',              N'Chest',     1),
        (N'Cough with Expectoration',   N'%COUGH%EXPECT%',             N'Chest',     1),
        (N'Shortness of Breath',        N'%DYSPN%',                    N'Chest',     1),
        (N'Shortness of Breath',        N'%BREATH%DIFFIC%',            N'Chest',     2),
        (N'Chest Tightness',            N'%CONSTRICT%CHEST%',          N'Chest',     1),
        (N'Chest Pain',                 N'%PAIN%CHEST%',               N'Chest',     1),
        (N'Nasal Congestion',           N'%NOSE%STOP%',                N'Nose',      1),
        (N'Nasal Congestion',           N'%CORYZA%',                   N'Nose',      2),
        (N'Sneezing',                   N'%SNEEZ%',                    N'Nose',      1),

        -- MUSculoskeletal / GENERAL
        (N'Joint Pain',                 N'%PAIN%JOINT%',               N'Extremities', 1),
        (N'Joint Pain',                 N'%ARTHR%',                    N'Extremities', 2),
        (N'Back Pain',                  N'%PAIN%BACK%',                N'Back',      1),
        (N'Back Pain',                  N'%LUMB%',                     N'Back',      2),
        (N'Muscle Pain',                N'%PAIN%MUSC%',                N'General',   1),
        (N'Stiffness',                  N'%STIFF%',                    N'General',   1),
        (N'Weakness',                   N'%WEAK%',                     N'General',   1),
        (N'Fatigue',                    N'%FATIG%',                    N'General',   1),
        (N'Fatigue',                    N'%PROSTR%',                   N'General',   2),

        -- SKIN
        (N'Itching',                    N'%ITCH%',                     N'Skin',      1),
        (N'Itching',                    N'%PRURIT%',                   N'Skin',      2),
        (N'Eruption',                   N'%ERUPT%',                    N'Skin',      1),
        (N'Urticaria',                  N'%URTICAR%',                  N'Skin',      1),
        (N'Dry Skin',                   N'%SKIN%DRY%',                 N'Skin',      1),

        -- URINARY / FEMALE (validation still applies via RubricGenderRule)
        (N'Burning Urination',          N'%BURN%URIN%',                N'Urinary',   1),
        (N'Frequent Urination',         N'%URIN%FREQ%',                N'Urinary',   1),
        (N'Urinary Incontinence',       N'%INCONTIN%URIN%',            N'Urinary',   1),

        -- CARDIAC (already have Palpitation in epilepsy seed)
        (N'Palpitation',                N'%PALPITAT%',                 N'Cardiac',   1),
        (N'Hypertension',               N'%HYPERTENS%',                N'Cardiac',   1)
    ) AS v(HomeopathicConceptPattern, SubSectionNamePattern, Domain, PriorityOrder)
)
INSERT INTO dbo.AIConceptMappingBootstrap
    (HomeopathicConceptPattern, SubSectionNamePattern, Domain, PriorityOrder, IsActive)
SELECT s.HomeopathicConceptPattern, s.SubSectionNamePattern, s.Domain, s.PriorityOrder, 1
FROM SeedRows s
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.AIConceptMappingBootstrap b
    WHERE b.HomeopathicConceptPattern = s.HomeopathicConceptPattern
      AND b.SubSectionNamePattern = s.SubSectionNamePattern
);

DECLARE @Inserted INT = @@ROWCOUNT;
DECLARE @Total INT = (SELECT COUNT(*) FROM dbo.AIConceptMappingBootstrap WHERE IsActive = 1);
PRINT CONCAT('705: Inserted ', @Inserted, ' new bootstrap pattern(s). Active total: ', @Total);
PRINT '=== 705 Common-case bootstrap patterns COMPLETE ===';
GO

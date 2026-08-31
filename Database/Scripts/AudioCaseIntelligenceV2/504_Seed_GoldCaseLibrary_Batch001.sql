-- Phase 6: Seed starter gold benchmark cases (expand to 250+ over time)

IF NOT EXISTS (SELECT 1 FROM dbo.GoldCaseLibrary WHERE Category = N'Epilepsy' AND Transcript LIKE N'%Vibration in hands%')
BEGIN
    INSERT INTO dbo.GoldCaseLibrary (Category, Transcript, SourceLanguage, DoctorRubricsJson, PrimaryRubricIdsJson, FinalRemedy, IsActive)
    VALUES
    (
        N'Epilepsy',
        N'Vibration in hands 10 seconds before every fit at night.',
        N'en',
        N'["aura before convulsion","vibration hands"]',
        N'[]',
        NULL,
        1
    ),
    (
        N'Psychological',
        N'Since father''s death she cannot sleep and weeps alone.',
        N'en',
        N'["grief","weeping","sleep disturbed"]',
        N'[]',
        NULL,
        1
    ),
    (
        N'Gastrointestinal',
        N'Burning pain in stomach worse after eating spicy food.',
        N'en',
        N'["burning stomach","eating agg"]',
        N'[]',
        NULL,
        1
    ),
    (
        N'Pediatric',
        N'Child screams before urination with red face.',
        N'en',
        N'["screaming before urination","red face"]',
        N'[]',
        NULL,
        1
    ),
    (
        N'Chronic',
        N'Joint pain worse in damp weather, better by warmth.',
        N'en',
        N'["joint pain damp agg","warm amel"]',
        N'[]',
        NULL,
        1
    );
END
GO

-- Initial metaphor seed (EN + HI + MR samples)
-- Run AFTER 003_Create_RubricMetaphorDictionary.sql

IF NOT EXISTS (SELECT 1 FROM dbo.RubricMetaphorDictionary WHERE NormalizedExpression = N'vibration before fit')
BEGIN
    INSERT INTO dbo.RubricMetaphorDictionary
        (PatientExpression, NormalizedExpression, ClinicalMeaning, RubricMeaning, Language, ConfidenceWeight, ApprovalStatus, IsActive)
    VALUES
        (N'vibration before fit', N'vibration before fit', N'Prodromal aura before convulsive episode', N'GENERALITIES - CONVULSIONS - aura', N'en', 0.92, N'Approved', 1),
        (N'vibration in hands before attack', N'vibration in hands before attack', N'Sensory aura in hands preceding seizure', N'GENERALITIES - CONVULSIONS - aura', N'en', 0.90, N'Approved', 1),
        (N'dropping things from hands before fit', N'dropping things from hands before fit', N'Motor aura — dropping objects before convulsion', N'EXTREMITIES - HAND - dropping things', N'en', 0.88, N'Approved', 1),
        (N'fits mostly at night', N'fits mostly at night', N'Nocturnal convulsive tendency', N'GENERALITIES - CONVULSIONS - night', N'en', 0.85, N'Approved', 1),
        (N'fear of darkness since childhood', N'fear of darkness since childhood', N'Phobia of dark — mental symptom', N'MIND - FEAR - dark', N'en', 0.87, N'Approved', 1),
        (N'ailments from grief', N'ailments from grief', N'Causation from bereavement', N'MIND - GRIEF - ailments from', N'en', 0.86, N'Approved', 1);
END
GO

-- Marathi patient expressions (translated clinical meaning in English)
IF NOT EXISTS (SELECT 1 FROM dbo.RubricMetaphorDictionary WHERE Language = N'mr' AND NormalizedExpression = N'hatat hath kamptat')
BEGIN
    INSERT INTO dbo.RubricMetaphorDictionary
        (PatientExpression, NormalizedExpression, ClinicalMeaning, RubricMeaning, Language, ConfidenceWeight, ApprovalStatus, IsActive)
    VALUES
        (N'हातात अचानक कंप सुरू होते', N'hatat achanak kamp suru hote', N'Sudden trembling in hands — prodromal aura', N'GENERALITIES - CONVULSIONS - aura', N'mr', 0.90, N'Approved', 1),
        (N'फिट येण्यापूर्वी हात कापतात', N'fit yenya purvi hat kapdat', N'Hands shake before convulsion', N'GENERALITIES - CONVULSIONS - aura', N'mr', 0.91, N'Approved', 1);
END
GO

-- Hindi patient expressions
IF NOT EXISTS (SELECT 1 FROM dbo.RubricMetaphorDictionary WHERE Language = N'hi' AND NormalizedExpression = N'daura se pehle kanpna')
BEGIN
    INSERT INTO dbo.RubricMetaphorDictionary
        (PatientExpression, NormalizedExpression, ClinicalMeaning, RubricMeaning, Language, ConfidenceWeight, ApprovalStatus, IsActive)
    VALUES
        (N'दौरे से पहले हाथ कांपते हैं', N'daure se pehle hath kampate hain', N'Trembling hands before seizure — aura', N'GENERALITIES - CONVULSIONS - aura', N'hi', 0.91, N'Approved', 1),
        (N'andhere se bahut darr lagta hai', N'andhere se bahut darr lagta hai', N'Intense fear of darkness', N'MIND - FEAR - dark', N'hi', 0.86, N'Approved', 1);
END
GO

PRINT 'Metaphor seed batch 001 applied.';
GO

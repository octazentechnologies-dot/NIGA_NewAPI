-- Initial alias seed — links patient phrases to SubSectionMaster rubrics
-- Run AFTER 004_Create_RubricAlias.sql
-- Uses dynamic lookup so SubSectionId resolves from your database rubric names

DECLARE @AuraSubSectionId INT =
    (SELECT TOP 1 SubSectionId FROM dbo.SubSectionMaster
     WHERE SubSectionName LIKE N'%CONVULSIONS%AURA%' OR SubSectionName LIKE N'%CONVULSION%AURA%'
     ORDER BY SubSectionId);

DECLARE @FearDarkSubSectionId INT =
    (SELECT TOP 1 SubSectionId FROM dbo.SubSectionMaster
     WHERE SubSectionName LIKE N'%FEAR%DARK%' OR SubSectionName LIKE N'%MIND%FEAR%dark%'
     ORDER BY SubSectionId);

IF @AuraSubSectionId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.RubricAlias WHERE NormalizedAlias = N'vibration before fit')
BEGIN
    INSERT INTO dbo.RubricAlias (SubSectionId, AliasText, NormalizedAlias, Language, AliasType, Weight, Source, IsActive)
    VALUES
        (@AuraSubSectionId, N'vibration before fit', N'vibration before fit', N'en', N'patient_phrase', 0.95, N'seed', 1),
        (@AuraSubSectionId, N'vibration in hands before attack', N'vibration in hands before attack', N'en', N'patient_phrase', 0.92, N'seed', 1),
        (@AuraSubSectionId, N'warning before convulsion', N'warning before convulsion', N'en', N'clinical', 0.90, N'seed', 1),
        (@AuraSubSectionId, N'prodromal aura', N'prodromal aura', N'en', N'clinical', 0.88, N'seed', 1);
END
GO

DECLARE @FearDarkId INT =
    (SELECT TOP 1 SubSectionId FROM dbo.SubSectionMaster
     WHERE SubSectionName LIKE N'%FEAR%DARK%' ORDER BY SubSectionId);

IF @FearDarkId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.RubricAlias WHERE NormalizedAlias = N'fear of darkness')
BEGIN
    INSERT INTO dbo.RubricAlias (SubSectionId, AliasText, NormalizedAlias, Language, AliasType, Weight, Source, IsActive)
    VALUES
        (@FearDarkId, N'fear of darkness', N'fear of darkness', N'en', N'patient_phrase', 0.90, N'seed', 1),
        (@FearDarkId, N'scared of dark', N'scared of dark', N'en', N'patient_phrase', 0.85, N'seed', 1);
END
GO

PRINT 'Alias seed batch 001 applied (skipped rows when rubrics not found in SubSectionMaster).';
GO

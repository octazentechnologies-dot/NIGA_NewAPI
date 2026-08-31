-- Phase 7: Complete repertory mapping only (safe to re-run; does not touch Kent maps)
-- Run when KentMaps > 0 but CompleteMaps = 0

DECLARE @KentSourceId BIGINT = (SELECT RepertorySourceId FROM dbo.RepertorySource WHERE SourceCode = N'KENT');
DECLARE @CompleteSourceId BIGINT = (SELECT RepertorySourceId FROM dbo.RepertorySource WHERE SourceCode = N'COMPLETE');

DECLARE @CompletePrimaryAuthorId INT =
    (SELECT TOP 1 am.AuthorId
     FROM dbo.AuthorMaster am
     LEFT JOIN dbo.RemedyRubricAuthorDetails rra
        ON rra.AuthorId = am.AuthorId AND ISNULL(rra.DeletedStatus, 0) = 0
     WHERE ISNULL(am.IsDeleted, 0) = 0
       AND (
            am.AuthorName LIKE N'%Complete%Repertory%'
         OR am.AuthorName LIKE N'%COMPLETE REPERTORY%'
         OR am.AuthorName LIKE N'%VAN ZANDVOORT%'
         OR am.AuthorName LIKE N'%ZANDVOORT%'
         OR am.AuthorAlias LIKE N'cr'
         OR am.AuthorAlias LIKE N'cr%'
         OR (am.AuthorName LIKE N'%Complete%' AND am.AuthorName NOT LIKE N'%Kent%')
       )
     GROUP BY am.AuthorId, am.AuthorName
     ORDER BY COUNT(rra.RubricRemedyId) DESC, am.AuthorId);

UPDATE dbo.RepertorySource
SET AuthorId = @CompletePrimaryAuthorId
WHERE SourceCode = N'COMPLETE';

IF @CompleteSourceId IS NOT NULL
BEGIN
    INSERT INTO dbo.RubricRepertoryMap (SubSectionId, RepertorySourceId, SourceRubricKey, SourceRubricPath, MappingConfidence, IsPrimarySource, IsActive)
    SELECT DISTINCT
        rr.SubSectionId,
        @CompleteSourceId,
        ss.SubSectionName,
        sm.SectionName,
        0.9500,
        CASE WHEN NOT EXISTS (
            SELECT 1 FROM dbo.RubricRepertoryMap km
            WHERE km.SubSectionId = rr.SubSectionId AND km.RepertorySourceId = @KentSourceId AND km.IsActive = 1)
        THEN 1 ELSE 0 END,
        1
    FROM dbo.RubricRemedyDetails rr
    INNER JOIN dbo.SubSectionMaster ss ON ss.SubSectionId = rr.SubSectionId AND ISNULL(ss.DeleteStatus, 0) = 0
    LEFT JOIN dbo.SectionMaster sm ON sm.SectionId = ss.SectionId
    INNER JOIN dbo.RemedyRubricAuthorDetails rra ON rra.RubricRemedyId = rr.RubricRemedyId AND ISNULL(rra.DeletedStatus, 0) = 0
    INNER JOIN dbo.AuthorMaster am ON am.AuthorId = rra.AuthorId AND ISNULL(am.IsDeleted, 0) = 0
    WHERE ISNULL(rr.DeletedStatus, 0) = 0
      AND rr.SubSectionId IS NOT NULL
      AND (
            am.AuthorName LIKE N'%Complete%Repertory%'
         OR am.AuthorName LIKE N'%COMPLETE REPERTORY%'
         OR am.AuthorName LIKE N'%VAN ZANDVOORT%'
         OR am.AuthorName LIKE N'%ZANDVOORT%'
         OR am.AuthorAlias LIKE N'cr'
         OR am.AuthorAlias LIKE N'cr%'
         OR (am.AuthorName LIKE N'%Complete%' AND am.AuthorName NOT LIKE N'%Kent%')
      )
      AND NOT EXISTS (
          SELECT 1 FROM dbo.RubricRepertoryMap m
          WHERE m.SubSectionId = rr.SubSectionId AND m.RepertorySourceId = @CompleteSourceId);
END
GO

DECLARE @CompleteMapped INT = (
    SELECT COUNT(*) FROM dbo.RubricRepertoryMap m
    JOIN dbo.RepertorySource s ON s.RepertorySourceId = m.RepertorySourceId
    WHERE s.SourceCode = N'COMPLETE' AND m.IsActive = 1);

PRINT CONCAT('Complete maps: ', @CompleteMapped);
GO

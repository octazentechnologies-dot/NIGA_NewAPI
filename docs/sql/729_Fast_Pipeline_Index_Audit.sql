/*
  729 — Fast pipeline index audit / safe CREATE for SubSectionMaster + embedding lookups.
  Phases 9 / 26. Does NOT DROP or DELETE data. Idempotent IF NOT EXISTS pattern.

  Review existing indexes on your environment before applying.
  FTS (CONTAINS) for SubSectionName was introduced in scripts 725/726 — do not recreate FTS here.

  NOTE: Do NOT index dbo.SubSectionMaster.SubSectionName as a KEY column.
  In production it is typically NVARCHAR(MAX) / LOB → Msg 1919.
  Name search is covered by FTS (725/726). SectionId index may INCLUDE the name.
*/

SET NOCOUNT ON;
GO

-- Supporting nonclustered index for Section-scoped browsing used by V1/hotspot paths.
-- SubSectionName is INCLUDE-only (LOB-safe); never a key column.
IF OBJECT_ID(N'dbo.SubSectionMaster', N'U') IS NOT NULL
   AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SubSectionMaster')
      AND name = N'IX_SubSectionMaster_SectionId_SubSectionId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_SubSectionMaster_SectionId_SubSectionId
        ON dbo.SubSectionMaster (SectionId, SubSectionId)
        INCLUDE (SubSectionName);
END
GO

-- Intentionally omitted:
--   CREATE INDEX ... ON dbo.SubSectionMaster (SubSectionName)
-- SubSectionName is not a valid index KEY type on this table (Msg 1919).
-- Use full-text catalog from scripts 725/726 for name search.

-- RubricEmbeddings: model + rubric lookup (query embedding cache is in-process; this is DB side)
IF OBJECT_ID(N'dbo.RubricEmbeddings', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.RubricEmbeddings')
          AND name = N'IX_RubricEmbeddings_Model_Rubric')
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricEmbeddings_Model_Rubric
        ON dbo.RubricEmbeddings (ModelName, RubricId)
        INCLUDE (TextHash);
END
GO

-- Intelligence log session/stage filter (baseline / fast-e telemetry)
IF OBJECT_ID(N'dbo.AudioCaseIntelligenceLog', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AudioCaseIntelligenceLog')
          AND name = N'IX_AudioCaseIntelligenceLog_Session_Stage')
BEGIN
    CREATE NONCLUSTERED INDEX IX_AudioCaseIntelligenceLog_Session_Stage
        ON dbo.AudioCaseIntelligenceLog (AudioCaseSessionId, StageName)
        INCLUDE (EngineVersion, Status, LatencyMs, EnteredDate);
END
GO

PRINT '729 fast pipeline index audit script completed (CREATE IF NOT EXISTS only).';
PRINT '729 note: SubSectionName key index skipped (LOB/MAX column — use FTS 725/726).';
GO

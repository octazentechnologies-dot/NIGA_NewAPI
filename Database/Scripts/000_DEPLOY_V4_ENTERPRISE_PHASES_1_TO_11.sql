/*
================================================================================
  HOMEOCENTRUM — V4 ENTERPRISE AI PIPELINE
  MASTER SQL DEPLOYMENT — Prerequisites + Phases 1–11

  Target database : HomeoCentrum_Production  (change USE below if different)
  Safe to re-run  : Child scripts use IF NOT EXISTS / idempotent checks
  Does NOT modify : SectionMaster, SubSectionMaster, RemedyMaster (repertory)

  HOW TO RUN (SSMS):
  ──────────────────
  1. Take a FULL database backup first.
  2. SQL Server Management Studio → Query → SQLCMD Mode  (must be ON)
  3. File → Open → this file:
       New_API/Database/Scripts/000_DEPLOY_V4_ENTERPRISE_PHASES_1_TO_11.sql
  4. Confirm connection points to the correct server/database.
  5. Press F5 to execute.  Watch Messages tab for PRINT output.

  ALTERNATIVE (sqlcmd from PowerShell):
  ─────────────────────────────────────
  sqlcmd -S YOUR_SERVER -d HomeoCentrum_Production -E -i "000_DEPLOY_V4_ENTERPRISE_PHASES_1_TO_11.sql"

  EXECUTION ORDER SUMMARY:
  ────────────────────────
  [Prereq]  Audio Case Taking V1 tables
  [V2 0–6]  Case understanding, metaphors, embeddings, inference, feedback
  [V2 7]    Repertory Kent + Complete mapping
  [V3]      Concept graph core (required for Phases 5–6)
  [V3.5]    Recall engine columns + tables
  [V2.1]    Clinical validation rules (Phase 7 enterprise validation)
  [V4 P1]   Enterprise embedding infrastructure (801)
  [V4 P3]   Incremental embedding sync state (802)
  [Phase 10] AI monitoring dashboard tables (711)
  [Phase 11] Enterprise knowledge graph tables (712)
  [Optional] Bootstrap seeds for concept graph / aliases

  AFTER SQL — deploy API + run post-deploy API calls (see deployment guide).
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- Uncomment and set your database name:
-- USE HomeoCentrum_Production;
-- GO

PRINT '';
PRINT '================================================================';
PRINT ' HOMEOCENTRUM V4 ENTERPRISE DEPLOY — START';
PRINT ' ' + CONVERT(VARCHAR(30), GETUTCDATE(), 120) + ' UTC';
PRINT '================================================================';
GO

/* ─────────────────────────────────────────────────────────────────────────────
   PREREQUISITE — Audio Case Taking V1 (session, events, match log)
   Skip-safe: uses IF NOT EXISTS
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> PREREQ | Audio Case Taking V1 base tables';
:r .\AudioCaseTaking_CreateTables.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   V2 PHASES 0–6 — consolidated (~21 scripts inline)
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> V2 Phases 0–6 | Case understanding through feedback/benchmark';
:r .\AudioCaseIntelligenceV2\000_DEPLOY_ALL_Phases_0_to_6.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   V2 PHASE 7 — Repertory Kent + Complete
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> V2 Phase 7 | Repertory source + RubricRepertoryMap';
:r .\AudioCaseIntelligenceV2\000_DEPLOY_Phase_7.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   V3 CONCEPT GRAPH — required for Enterprise Phase 5 (Multi-Concept Discovery)
   Includes: AIPatientMeaning, AIClinicalConcept, AICaseLearning, etc.
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> V3 | Concept graph core tables';
:r .\AudioCaseIntelligenceV2\000_DEPLOY_V3_ALL.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   V3.5 RECALL ENGINE — transcript coverage, symptom blocks, pattern discovery
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> V3.5 | Recall optimization engine';
:r .\AudioCaseIntelligenceV2\000_DEPLOY_V3_5_ALL.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   V2.1 CLINICAL VALIDATION — required for Enterprise Phase 7
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> V2.1 | Clinical validation rules + validation log';
:r .\AudioCaseIntelligenceV2\601_Create_RubricValidationRules.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   V4 PHASE 1 — Enterprise embedding infrastructure (AIRubricEmbedding, queue, etc.)
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> V4 Phase 1 | Enterprise embedding infrastructure';
:r .\AIV4EmbeddingInfrastructure\801_Create_AIEmbeddingInfrastructure.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   V4 PHASE 3 — Incremental embedding refresh sync state
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> V4 Phase 3 | Incremental embedding sync state';
:r .\AIV4EmbeddingInfrastructure\802_Create_AIEmbeddingSyncState.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   ENTERPRISE PHASE 10 — AI Monitoring Dashboard
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> Enterprise Phase 10 | AI monitoring dashboard tables';
:r .\AudioCaseIntelligenceV2\711_Create_AIMonitoring_Dashboard.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   ENTERPRISE PHASE 11 — Homeopathic Knowledge Graph
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> Enterprise Phase 11 | Enterprise knowledge graph tables';
:r .\AudioCaseIntelligenceV2\712_Create_EnterpriseKnowledgeGraph.sql
GO

/* ─────────────────────────────────────────────────────────────────────────────
   OPTIONAL SEEDS — uncomment if you want bootstrap concept graph data
   ───────────────────────────────────────────────────────────────────────────── */
-- PRINT '';
-- PRINT '>>> OPTIONAL | V3 concept graph bootstrap seed';
-- :r .\AudioCaseIntelligenceV2\703_Seed_AIConceptGraph_Bootstrap.sql
-- GO

-- PRINT '';
-- PRINT '>>> OPTIONAL | Common case concept mapping bootstrap';
-- :r .\AudioCaseIntelligenceV2\705_Seed_AIConceptMappingBootstrap_CommonCases.sql
-- GO

-- PRINT '';
-- PRINT '>>> OPTIONAL | Concept alias bootstrap';
-- :r .\AudioCaseIntelligenceV2\707_Seed_Bootstrap_ConceptAliases.sql
-- GO

/* ─────────────────────────────────────────────────────────────────────────────
   POST-DEPLOY VERIFICATION
   ───────────────────────────────────────────────────────────────────────────── */
PRINT '';
PRINT '>>> VERIFICATION | Table existence checks';
GO

SELECT
    ObjectName,
    CASE WHEN EXISTS (
        SELECT 1 FROM sys.tables t
        WHERE t.name = v.ObjectName AND t.schema_id = SCHEMA_ID(N'dbo')
    ) THEN 'OK' ELSE 'MISSING' END AS [Status]
FROM (VALUES
    (N'AudioCaseSession'),
    (N'AudioCaseClinicalConcept'),
    (N'RubricEmbeddings'),
    (N'AudioCaseRubricFeedback'),
    (N'RepertorySource'),
    (N'AIPatientMeaning'),
    (N'AIClinicalConcept'),
    (N'AICaseLearning'),
    (N'AISymptomBlock'),
    (N'AudioCaseRubricValidationLog'),
    (N'AIEmbeddingVersion'),
    (N'AIRubricEmbedding'),
    (N'AIEmbeddingSyncState'),
    (N'AIMonitoringDailySnapshot'),
    (N'AIMonitoringAuditLog'),
    (N'AIKGNode'),
    (N'AIKGEdge'),
    (N'AIKGRemedyProjection')
) v(ObjectName)
ORDER BY ObjectName;
GO

PRINT '';
PRINT '================================================================';
PRINT ' HOMEOCENTRUM V4 ENTERPRISE DEPLOY — FINISHED';
PRINT ' Next: deploy New_API + NigaHomeopathy-UI, then run post-deploy API steps.';
PRINT '================================================================';
GO

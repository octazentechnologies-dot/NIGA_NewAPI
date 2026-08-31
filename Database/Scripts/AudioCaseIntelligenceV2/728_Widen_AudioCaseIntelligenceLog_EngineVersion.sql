-- =============================================================================
-- 728_Widen_AudioCaseIntelligenceLog_EngineVersion.sql
-- Fix: Stage A telemetry used EngineVersion longer than NVARCHAR(10), causing
-- SqlException (String or binary data would be truncated) → EF "An error occurred
-- while saving the entity changes" and poisoned DbContext for the session.
-- Safe: widen only; no data deletion.
-- =============================================================================

BEGIN TRANSACTION;
BEGIN TRY
    IF COL_LENGTH('dbo.AudioCaseIntelligenceLog', 'EngineVersion') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.AudioCaseIntelligenceLog
            ALTER COLUMN EngineVersion NVARCHAR(50) NOT NULL;

        -- Recreate default if SQL Server dropped it on ALTER (common on some versions)
        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AudioCaseIntelligenceLog')
              AND c.name = N'EngineVersion'
        )
        BEGIN
            ALTER TABLE dbo.AudioCaseIntelligenceLog
                ADD CONSTRAINT DF_AudioCaseIntelligenceLog_EngineVersion
                DEFAULT ('v2') FOR EngineVersion;
        END
    END

    COMMIT TRANSACTION;
    PRINT '728: AudioCaseIntelligenceLog.EngineVersion widened to NVARCHAR(50).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @Err NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'728 failed: %s', 16, 1, @Err);
END CATCH;
GO

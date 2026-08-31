-- Phase 3: Causation links JSON cache on session (for GET /concepts without DB join)

IF COL_LENGTH('dbo.AudioCaseSession', 'CausationLinksJson') IS NULL
BEGIN
    ALTER TABLE dbo.AudioCaseSession
        ADD CausationLinksJson NVARCHAR(MAX) NULL;
END
GO

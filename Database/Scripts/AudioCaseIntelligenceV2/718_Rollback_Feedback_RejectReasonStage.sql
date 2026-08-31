IF COL_LENGTH('dbo.AudioCaseRubricFeedback', 'RejectReasonNote') IS NOT NULL
    ALTER TABLE dbo.AudioCaseRubricFeedback DROP COLUMN RejectReasonNote;
GO

IF COL_LENGTH('dbo.AudioCaseRubricFeedback', 'RejectReasonStage') IS NOT NULL
    ALTER TABLE dbo.AudioCaseRubricFeedback DROP COLUMN RejectReasonStage;
GO

IF COL_LENGTH('dbo.AIDoctorFeedback', 'RejectReasonStage') IS NOT NULL
    ALTER TABLE dbo.AIDoctorFeedback DROP COLUMN RejectReasonStage;
GO

IF COL_LENGTH('dbo.AIDoctorFeedback', 'RejectReasonNote') IS NOT NULL
    ALTER TABLE dbo.AIDoctorFeedback DROP COLUMN RejectReasonNote;
GO

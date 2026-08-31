IF COL_LENGTH('dbo.AudioCaseRubricFeedback', 'RejectReasonStage') IS NULL
    ALTER TABLE dbo.AudioCaseRubricFeedback ADD RejectReasonStage NVARCHAR(40) NULL;
GO

IF COL_LENGTH('dbo.AudioCaseRubricFeedback', 'RejectReasonNote') IS NULL
    ALTER TABLE dbo.AudioCaseRubricFeedback ADD RejectReasonNote NVARCHAR(1000) NULL;
GO

IF COL_LENGTH('dbo.AIDoctorFeedback', 'RejectReasonStage') IS NULL
    ALTER TABLE dbo.AIDoctorFeedback ADD RejectReasonStage NVARCHAR(40) NULL;
GO

IF COL_LENGTH('dbo.AIDoctorFeedback', 'RejectReasonNote') IS NULL
    ALTER TABLE dbo.AIDoctorFeedback ADD RejectReasonNote NVARCHAR(1000) NULL;
GO

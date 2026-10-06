/*
  S5 — doctor reply on a patient review (My Reviews panel on the doctor profile)
  and admin moderation note (Trust & Verification review queue).
  Idempotent: safe to run more than once.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Review', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.Review is missing. Run the S4 Week 4 schema script first.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.Review', N'DoctorReply') IS NULL
    ALTER TABLE dbo.Review ADD DoctorReply nvarchar(500) NULL;
GO

IF COL_LENGTH(N'dbo.Review', N'RepliedAt') IS NULL
    ALTER TABLE dbo.Review ADD RepliedAt datetime NULL;
GO

IF COL_LENGTH(N'dbo.Review', N'ModerationNote') IS NULL
    ALTER TABLE dbo.Review ADD ModerationNote nvarchar(500) NULL;
GO

IF COL_LENGTH(N'dbo.Review', N'ModeratedAt') IS NULL
    ALTER TABLE dbo.Review ADD ModeratedAt datetime NULL;
GO

PRINT N'06_S5_Review_Doctor_Reply applied.';
GO

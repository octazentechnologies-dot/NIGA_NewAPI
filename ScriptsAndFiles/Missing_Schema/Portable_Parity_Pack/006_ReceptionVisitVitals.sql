/*
Purpose: dbo.ReceptionVisitVitals — reception visit vitals JSON store.
Source evidence: HomeoCentrum_Dev object definition + S3Week3Service usage.
Idempotent. No sample rows.
Risk: Low (additive).
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ReceptionVisitVitals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceptionVisitVitals (
        ReceptionVisitVitalsId int IDENTITY(1,1) NOT NULL,
        PatientAppId int NOT NULL,
        PatientId int NOT NULL,
        DoctorId int NOT NULL,
        VitalsJson nvarchar(max) NOT NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_ReceptionVisitVitals_CreatedAt DEFAULT (GETDATE()),
        UpdatedAt datetime NOT NULL CONSTRAINT DF_ReceptionVisitVitals_UpdatedAt DEFAULT (GETDATE()),
        UpdatedBy bigint NULL,
        CONSTRAINT PK_ReceptionVisitVitals PRIMARY KEY CLUSTERED (ReceptionVisitVitalsId)
    );
    CREATE UNIQUE INDEX UX_ReceptionVisitVitals_PatientAppId
        ON dbo.ReceptionVisitVitals (PatientAppId);
    PRINT 'Created dbo.ReceptionVisitVitals';
END
ELSE
    PRINT 'dbo.ReceptionVisitVitals already exists';
GO

PRINT '006_ReceptionVisitVitals completed.';
GO

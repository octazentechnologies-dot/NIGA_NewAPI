/*
    HomeoCentrum - Doctor Patient Board work backup (one latest backup per doctor user)
    Database: HomeoCentrum_Production
    Run manually before deploying PatientBoardBackup API.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = N'DoctorPatientBoardBackup'
      AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.DoctorPatientBoardBackup
    (
        BackupId BIGINT IDENTITY(1, 1) NOT NULL,
        DoctorUserId BIGINT NOT NULL,
        BackupPayload NVARCHAR(MAX) NOT NULL,
        PatientCount INT NOT NULL CONSTRAINT DF_DoctorPatientBoardBackup_PatientCount DEFAULT (0),
        SchemaVersion INT NOT NULL CONSTRAINT DF_DoctorPatientBoardBackup_SchemaVersion DEFAULT (1),
        EnteredBy INT NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_DoctorPatientBoardBackup_EnteredDate DEFAULT (GETDATE()),
        ChangedBy INT NULL,
        ChangedDate DATETIME NULL,
        DeleteStatus BIT NOT NULL CONSTRAINT DF_DoctorPatientBoardBackup_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_DoctorPatientBoardBackup PRIMARY KEY CLUSTERED (BackupId)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UX_DoctorPatientBoardBackup_DoctorUserId_Active
        ON dbo.DoctorPatientBoardBackup (DoctorUserId)
        WHERE DeleteStatus = 0;
END
GO

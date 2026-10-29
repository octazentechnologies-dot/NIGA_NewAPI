-- Doctor app → web portal handoff.
-- Stores the hash of a 60-second one-time code and the short web session it opens.
-- The raw code and the JWT are never written here.
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.WebHandoffSession', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebHandoffSession
    (
        WebHandoffSessionId BIGINT IDENTITY(1,1) NOT NULL,
        Sid UNIQUEIDENTIFIER NOT NULL,
        CodeHash VARCHAR(64) NOT NULL,
        DoctorUserId BIGINT NOT NULL,
        DoctorId INT NOT NULL,
        Purpose NVARCHAR(40) NOT NULL,
        PatientId INT NULL,
        CaseId INT NULL,
        PatientAppId INT NULL,
        AppointmentDate NVARCHAR(40) NULL,
        VisitType NVARCHAR(40) NULL,
        ConsultMode NVARCHAR(40) NULL,
        IsTele BIT NOT NULL CONSTRAINT DF_WebHandoffSession_IsTele DEFAULT (0),
        ReturnUrl NVARCHAR(300) NOT NULL,
        CodeExpiresAtUtc DATETIME2 NOT NULL,
        ConsumedAtUtc DATETIME2 NULL,
        AbsoluteExpiresAtUtc DATETIME2 NULL,
        LastActivityAtUtc DATETIME2 NULL,
        CurrentJti NVARCHAR(64) NULL,
        RevokedAtUtc DATETIME2 NULL,
        RevokeReason NVARCHAR(40) NULL,
        CreatedFromIp NVARCHAR(64) NULL,
        EnteredDate DATETIME2 NOT NULL CONSTRAINT DF_WebHandoffSession_EnteredDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_WebHandoffSession PRIMARY KEY CLUSTERED (WebHandoffSessionId)
    );
    PRINT 'Created dbo.WebHandoffSession';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_WebHandoffSession_Sid' AND object_id = OBJECT_ID(N'dbo.WebHandoffSession')
)
    CREATE UNIQUE INDEX UX_WebHandoffSession_Sid ON dbo.WebHandoffSession (Sid);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_WebHandoffSession_CodeHash' AND object_id = OBJECT_ID(N'dbo.WebHandoffSession')
)
    CREATE UNIQUE INDEX UX_WebHandoffSession_CodeHash ON dbo.WebHandoffSession (CodeHash);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_WebHandoffSession_Doctor_Entered' AND object_id = OBJECT_ID(N'dbo.WebHandoffSession')
)
    CREATE INDEX IX_WebHandoffSession_Doctor_Entered ON dbo.WebHandoffSession (DoctorUserId, EnteredDate);
GO

IF OBJECT_ID(N'dbo.WebHandoffJti', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebHandoffJti
    (
        Jti NVARCHAR(64) NOT NULL,
        Sid UNIQUEIDENTIFIER NOT NULL,
        ExpiresAtUtc DATETIME2 NOT NULL,
        ReplacedAtUtc DATETIME2 NULL,
        CONSTRAINT PK_WebHandoffJti PRIMARY KEY CLUSTERED (Jti)
    );
    CREATE INDEX IX_WebHandoffJti_Sid ON dbo.WebHandoffJti (Sid);
    PRINT 'Created dbo.WebHandoffJti';
END
GO

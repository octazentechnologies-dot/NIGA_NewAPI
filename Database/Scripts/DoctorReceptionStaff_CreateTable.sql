/*
    HomeoCentrum - Doctor Reception Staff table
    Database: HomeoCentrum_Production
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = N'DoctorReceptionStaff'
      AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.DoctorReceptionStaff
    (
        ReceptionStaffID INT IDENTITY(1, 1) NOT NULL,
        DoctorID INT NOT NULL,
        UserID NVARCHAR(100) NOT NULL,
        Password NVARCHAR(500) NOT NULL,
        FullName NVARCHAR(250) NOT NULL,
        Address NVARCHAR(MAX) NULL,
        ContactNumber NVARCHAR(50) NOT NULL,
        EmailId NVARCHAR(250) NULL,
        Country NVARCHAR(100) NULL,
        State NVARCHAR(100) NULL,
        City NVARCHAR(100) NULL,
        EnteredBy INT NULL,
        EnteredDate DATETIME NOT NULL,
        ChangedBy INT NULL,
        ChangedDate DATETIME NULL,
        DeleteStatus BIT NOT NULL CONSTRAINT DF_DoctorReceptionStaff_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_DoctorReceptionStaff PRIMARY KEY CLUSTERED (ReceptionStaffID),
        CONSTRAINT FK_DoctorReceptionStaff_Doctor FOREIGN KEY (DoctorID)
            REFERENCES dbo.Doctor (DoctorID)
    );

    CREATE NONCLUSTERED INDEX IX_DoctorReceptionStaff_DoctorID
        ON dbo.DoctorReceptionStaff (DoctorID);

    CREATE NONCLUSTERED INDEX IX_DoctorReceptionStaff_UserID
        ON dbo.DoctorReceptionStaff (UserID);

    CREATE NONCLUSTERED INDEX IX_DoctorReceptionStaff_ContactNumber
        ON dbo.DoctorReceptionStaff (ContactNumber);

    CREATE NONCLUSTERED INDEX IX_DoctorReceptionStaff_EmailId
        ON dbo.DoctorReceptionStaff (EmailId)
        WHERE EmailId IS NOT NULL;
END
GO

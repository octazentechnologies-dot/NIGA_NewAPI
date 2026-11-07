/*
  Pharmacy onboarding expansion:
  - Owner name + structured address (signup masters)
  - Multi licence documents (SecureDocument links)
  - Lifecycle: PENDING / ACTIVE / REJECTED / CLOSE_REQUESTED / CLOSED / SUSPENDED
  - Decision reason for reject / close

  Apply after Production COPY_ONLY backup (see .cursor/rules/production-db-backup.mdc).
*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.PharmacyPartner', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.PharmacyPartner is missing. Apply S4 Week4 schema first.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.PharmacyPartner', N'OwnerName') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD OwnerName NVARCHAR(200) NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'CountryId') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD CountryId INT NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'StateId') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD StateId INT NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'DistrictId') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD DistrictId INT NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'CityId') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD CityId INT NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'PinCodeId') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD PinCodeId INT NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'AddressLine1') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD AddressLine1 NVARCHAR(250) NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'AddressLine2') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD AddressLine2 NVARCHAR(250) NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'Landmark') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD Landmark NVARCHAR(200) NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'PostalCode') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD PostalCode NVARCHAR(20) NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'DecisionReason') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD DecisionReason NVARCHAR(500) NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'DecidedAt') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD DecidedAt DATETIME NULL;
GO
IF COL_LENGTH(N'dbo.PharmacyPartner', N'UpdatedAt') IS NULL
    ALTER TABLE dbo.PharmacyPartner ADD UpdatedAt DATETIME NULL;
GO

IF OBJECT_ID(N'dbo.PharmacyLicenceDocument', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PharmacyLicenceDocument
    (
        PharmacyLicenceDocumentId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_PharmacyLicenceDocument PRIMARY KEY,
        PharmacyPartnerId INT NOT NULL,
        PharmacyLicenceId INT NULL,
        SecureDocumentId BIGINT NOT NULL,
        DocumentType NVARCHAR(40) NOT NULL
            CONSTRAINT DF_PharmacyLicenceDocument_Type DEFAULT (N'Licence'),
        CreatedAt DATETIME NOT NULL
            CONSTRAINT DF_PharmacyLicenceDocument_CreatedAt DEFAULT (GETDATE()),
        CreatedBy BIGINT NULL
    );
    CREATE INDEX IX_PharmacyLicenceDocument_Partner
        ON dbo.PharmacyLicenceDocument (PharmacyPartnerId, CreatedAt DESC);
END
GO

PRINT N'18_Pharmacy_Onboarding_Full applied.';
GO

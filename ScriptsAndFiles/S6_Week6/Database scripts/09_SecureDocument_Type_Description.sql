/*
  SecureDocument metadata for patient uploads:
  - DocumentType (Report / Prescription / LabReport / Invoice / Other)
  - Description (optional)
  Run on HomeoCentrum_Dev and HomeoCentrum_Stage.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.SecureDocument', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.SecureDocument is missing.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.SecureDocument', N'DocumentType') IS NULL
BEGIN
    ALTER TABLE dbo.SecureDocument ADD DocumentType NVARCHAR(40) NULL;
    PRINT 'Added SecureDocument.DocumentType';
END
ELSE
    PRINT 'SecureDocument.DocumentType OK';

IF COL_LENGTH(N'dbo.SecureDocument', N'Description') IS NULL
BEGIN
    ALTER TABLE dbo.SecureDocument ADD Description NVARCHAR(500) NULL;
    PRINT 'Added SecureDocument.Description';
END
ELSE
    PRINT 'SecureDocument.Description OK';
GO

SET NOCOUNT ON;
/* Backfill empty types to Other for patient-owned rows (separate batch after ALTER). */
IF COL_LENGTH(N'dbo.SecureDocument', N'DocumentType') IS NOT NULL
BEGIN
    UPDATE dbo.SecureDocument
    SET DocumentType = N'Other'
    WHERE OwnerType = N'Patient'
      AND (DocumentType IS NULL OR LTRIM(RTRIM(DocumentType)) = N'');
    PRINT CONCAT('Backfilled DocumentType Other: ', @@ROWCOUNT);
END
GO

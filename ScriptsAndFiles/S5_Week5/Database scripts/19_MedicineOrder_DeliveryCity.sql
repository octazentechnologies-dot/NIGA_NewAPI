/*
  Snapshot delivery city on medicine orders so multi-area pharmacies
  always know fulfilment destination (patient address city).
  Apply after Production COPY_ONLY backup when targeting Production.
*/
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.MedicineOrder', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.MedicineOrder is missing.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.MedicineOrder', N'DeliveryAddressId') IS NULL
    ALTER TABLE dbo.MedicineOrder ADD DeliveryAddressId INT NULL;
GO

IF COL_LENGTH(N'dbo.MedicineOrder', N'DeliveryCity') IS NULL
    ALTER TABLE dbo.MedicineOrder ADD DeliveryCity NVARCHAR(120) NULL;
GO

PRINT N'19_MedicineOrder_DeliveryCity applied.';
GO

/*
  Per-order delivery fee for medicine quotes / Order Details (PAT-MED-OD-API).
  Nullable — never invent 0 for unknown. Run with sqlcmd -I on Dev, Stage, Production.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.MedicineOrder', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.MedicineOrder is missing.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.MedicineOrder', N'DeliveryFee') IS NULL
BEGIN
    ALTER TABLE dbo.MedicineOrder ADD DeliveryFee DECIMAL(18,2) NULL;
    PRINT 'Added MedicineOrder.DeliveryFee';
END
ELSE
    PRINT 'MedicineOrder.DeliveryFee OK';
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.MedicineQuote', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.MedicineQuote is missing.', 16, 1);
    RETURN;
END;

IF COL_LENGTH(N'dbo.MedicineQuote', N'DeliveryFee') IS NULL
BEGIN
    ALTER TABLE dbo.MedicineQuote ADD DeliveryFee DECIMAL(18,2) NULL;
    PRINT 'Added MedicineQuote.DeliveryFee';
END
ELSE
    PRINT 'MedicineQuote.DeliveryFee OK';
GO

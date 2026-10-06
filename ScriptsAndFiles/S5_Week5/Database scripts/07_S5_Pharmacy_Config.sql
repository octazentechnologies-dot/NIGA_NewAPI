/*
  S5 — pharmacy operating configuration (MED-05.03): working days, delivery charges and capacity.
  Hours and service areas stay in dbo.SellerRoutingRule (one row per area), which seller routing reads.
  Idempotent: safe to run more than once.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PharmacyPartner', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.PharmacyPartner is missing. Run the S4 Week 4 schema script first.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID(N'dbo.PharmacyConfig', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PharmacyConfig
    (
        PharmacyPartnerId int           NOT NULL CONSTRAINT PK_PharmacyConfig PRIMARY KEY,
        OpenTime          time          NOT NULL,
        CloseTime         time          NOT NULL,
        WorkingDays       nvarchar(40)  NOT NULL,
        DeliveryCharge    decimal(10,2) NOT NULL CONSTRAINT DF_PharmacyConfig_DeliveryCharge DEFAULT (0),
        FreeDeliveryAbove decimal(10,2) NULL,
        Capacity          int           NOT NULL,
        UpdatedAt         datetime      NOT NULL CONSTRAINT DF_PharmacyConfig_UpdatedAt DEFAULT (GETDATE()),
        UpdatedByUserId   bigint        NULL,
        CONSTRAINT FK_PharmacyConfig_Partner FOREIGN KEY (PharmacyPartnerId) REFERENCES dbo.PharmacyPartner (PharmacyPartnerId)
    );
END
GO

PRINT N'07_S5_Pharmacy_Config applied.';
GO

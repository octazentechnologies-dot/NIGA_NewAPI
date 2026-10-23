/*
Script       : 011_PROD_Seed_Pharmacy_Partner.sql
Purpose      : Ensure at least one ACTIVE PharmacyPartner exists (for medicine-order routing / pharmacy portal).
               Prefer linking Gourav_Dev_Pharmacy UserMaster when present.
Idempotent   : Safe to re-run.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.PharmacyPartner', N'U') IS NULL
BEGIN
    RAISERROR(N'dbo.PharmacyPartner is missing. Run S4 Week 4 schema first.', 16, 1);
    RETURN;
END

DECLARE @PharmacyUserId BIGINT = (
    SELECT TOP 1 UserId
    FROM dbo.UserMaster
    WHERE UserName = N'Gourav_Dev_Pharmacy'
      AND ISNULL(DeleteStatus, 0) = 0
);

DECLARE @PartnerId INT = (
    SELECT TOP 1 PharmacyPartnerId
    FROM dbo.PharmacyPartner
    WHERE Status <> N'DELETED'
      AND (
            (@PharmacyUserId IS NOT NULL AND UserId = @PharmacyUserId)
         OR Name = N'Gourav HomeoMeds Pharmacy'
         OR Name = N'Demo HomeoMeds'
      )
    ORDER BY CASE WHEN @PharmacyUserId IS NOT NULL AND UserId = @PharmacyUserId THEN 0 ELSE 1 END,
             PharmacyPartnerId
);

BEGIN TRAN;

IF @PartnerId IS NULL
BEGIN
    INSERT INTO dbo.PharmacyPartner (UserId, Name, Mobile, Area, Status, CreatedAt)
    VALUES (
        @PharmacyUserId,
        N'Gourav HomeoMeds Pharmacy',
        N'8001001005',
        N'Pune',
        N'ACTIVE',
        GETDATE()
    );
    SET @PartnerId = SCOPE_IDENTITY();
    PRINT CONCAT('INSERTED PharmacyPartner Id=', @PartnerId, ' UserId=', ISNULL(CONVERT(varchar(20), @PharmacyUserId), 'NULL'));
END
ELSE
BEGIN
    UPDATE dbo.PharmacyPartner
    SET Status = N'ACTIVE',
        Name = CASE WHEN Name IS NULL OR LTRIM(RTRIM(Name)) = N'' THEN N'Gourav HomeoMeds Pharmacy' ELSE Name END,
        Mobile = CASE WHEN Mobile IS NULL OR LTRIM(RTRIM(Mobile)) = N'' THEN N'8001001005' ELSE Mobile END,
        Area = CASE WHEN Area IS NULL OR LTRIM(RTRIM(Area)) = N'' THEN N'Pune' ELSE Area END,
        UserId = CASE WHEN UserId IS NULL AND @PharmacyUserId IS NOT NULL THEN @PharmacyUserId ELSE UserId END
    WHERE PharmacyPartnerId = @PartnerId;
    PRINT CONCAT('UPDATED PharmacyPartner Id=', @PartnerId, ' -> ACTIVE');
END

IF OBJECT_ID(N'dbo.PharmacyLicence', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.PharmacyLicence
        WHERE PharmacyPartnerId = @PartnerId AND Status = N'ACTIVE'
   )
BEGIN
    INSERT INTO dbo.PharmacyLicence (PharmacyPartnerId, LicenceNumber, ExpiryDate, Status)
    VALUES (@PartnerId, N'GHP-LIC-001', DATEADD(YEAR, 2, CAST(GETDATE() AS date)), N'ACTIVE');
    PRINT CONCAT('INSERTED PharmacyLicence for PartnerId=', @PartnerId);
END

IF OBJECT_ID(N'dbo.SellerRoutingRule', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.SellerRoutingRule WHERE PharmacyPartnerId = @PartnerId
   )
BEGIN
    INSERT INTO dbo.SellerRoutingRule (PharmacyPartnerId, Area, OpenTime, CloseTime, Capacity)
    VALUES (@PartnerId, N'Pune', CAST(N'09:00' AS time(0)), CAST(N'21:00' AS time(0)), 50);
    PRINT CONCAT('INSERTED SellerRoutingRule for PartnerId=', @PartnerId);
END

IF OBJECT_ID(N'dbo.PharmacyConfig', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.PharmacyConfig WHERE PharmacyPartnerId = @PartnerId
   )
BEGIN
    INSERT INTO dbo.PharmacyConfig (
        PharmacyPartnerId, OpenTime, CloseTime, WorkingDays,
        DeliveryCharge, FreeDeliveryAbove, Capacity, UpdatedAt, UpdatedByUserId
    )
    VALUES (
        @PartnerId,
        CAST(N'09:00' AS time(0)),
        CAST(N'21:00' AS time(0)),
        N'Mon,Tue,Wed,Thu,Fri,Sat',
        40.00,
        500.00,
        50,
        GETDATE(),
        @PharmacyUserId
    );
    PRINT CONCAT('INSERTED PharmacyConfig for PartnerId=', @PartnerId);
END

COMMIT TRAN;

SELECT
    p.PharmacyPartnerId,
    p.UserId,
    p.Name,
    p.Mobile,
    p.Area,
    p.Status,
    (SELECT COUNT(*) FROM dbo.PharmacyLicence l WHERE l.PharmacyPartnerId = p.PharmacyPartnerId) AS Licences,
    (SELECT COUNT(*) FROM dbo.SellerRoutingRule r WHERE r.PharmacyPartnerId = p.PharmacyPartnerId) AS RoutingRules,
    CASE WHEN OBJECT_ID(N'dbo.PharmacyConfig', N'U') IS NULL THEN 0
         ELSE (SELECT COUNT(*) FROM dbo.PharmacyConfig c WHERE c.PharmacyPartnerId = p.PharmacyPartnerId)
    END AS ConfigRows
FROM dbo.PharmacyPartner p
WHERE p.PharmacyPartnerId = @PartnerId;

PRINT '011_PROD_Seed_Pharmacy_Partner.sql completed.';
GO

/*
  Seed PatientDeliveryAddress from Patient chart address (registration / create time).

  Rules:
  - Only patients with no existing PatientDeliveryAddress rows
  - Requires AddressLine1 (or legacy Address) + a 6-digit PinCodeMaster.PinCode
  - Inserted row is Label='Home', IsDefault=1
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PatientDeliveryAddress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientDeliveryAddress (
        DeliveryAddressId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PatientId INT NOT NULL,
        Label NVARCHAR(80) NULL,
        Line1 NVARCHAR(200) NOT NULL,
        Line2 NVARCHAR(200) NULL,
        City NVARCHAR(80) NULL,
        State NVARCHAR(80) NULL,
        Pincode NVARCHAR(12) NOT NULL,
        Phone NVARCHAR(30) NULL,
        IsDefault BIT NOT NULL CONSTRAINT DF_Pda_Default_08 DEFAULT (0),
        CreatedAt DATETIME NOT NULL
    );
    PRINT 'Created dbo.PatientDeliveryAddress';
END;

IF COL_LENGTH(N'dbo.PatientDeliveryAddress', N'Phone') IS NULL
BEGIN
    ALTER TABLE dbo.PatientDeliveryAddress ADD Phone NVARCHAR(30) NULL;
    PRINT 'Added PatientDeliveryAddress.Phone';
END;

;WITH ProfileAddr AS (
    SELECT
        p.PatientId,
        LEFT(LTRIM(RTRIM(COALESCE(NULLIF(p.AddressLine1, N''), NULLIF(p.Address, N'')))), 200) AS Line1,
        LEFT(LTRIM(RTRIM(
            CASE
                WHEN NULLIF(LTRIM(RTRIM(p.AddressLine2)), N'') IS NOT NULL
                 AND NULLIF(LTRIM(RTRIM(p.Landmark)), N'') IS NOT NULL
                 AND LTRIM(RTRIM(p.AddressLine2)) <> LTRIM(RTRIM(p.Landmark))
                    THEN LTRIM(RTRIM(p.AddressLine2)) + N', ' + LTRIM(RTRIM(p.Landmark))
                ELSE COALESCE(NULLIF(LTRIM(RTRIM(p.AddressLine2)), N''), NULLIF(LTRIM(RTRIM(p.Landmark)), N''))
            END
        )), 200) AS Line2,
        LEFT(LTRIM(RTRIM(c.CityName)), 80) AS City,
        LEFT(LTRIM(RTRIM(s.StateName)), 80) AS State,
        LEFT(pin.PinCode, 12) AS Pincode,
        LEFT(LTRIM(RTRIM(COALESCE(NULLIF(p.MobileNo, N''), NULLIF(p.PhoneNo, N'')))), 30) AS Phone
    FROM dbo.Patient p
    LEFT JOIN dbo.PinCodeMaster pin ON pin.PinCodeId = p.PinCodeId
    LEFT JOIN dbo.CityMaster c ON c.CityId = p.CityId
    LEFT JOIN dbo.StateMaster s ON s.StateId = p.StateId
    WHERE ISNULL(p.DeleteStatus, 0) = 0
      AND NULLIF(LTRIM(RTRIM(COALESCE(p.AddressLine1, p.Address))), N'') IS NOT NULL
      AND pin.PinCode IS NOT NULL
      AND LEN(pin.PinCode) = 6
      AND pin.PinCode NOT LIKE N'%[^0-9]%'
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.PatientDeliveryAddress d
          WHERE d.PatientId = p.PatientId
      )
)
INSERT INTO dbo.PatientDeliveryAddress
    (PatientId, Label, Line1, Line2, City, State, Pincode, Phone, IsDefault, CreatedAt)
SELECT
    PatientId,
    N'Home',
    Line1,
    Line2,
    City,
    State,
    Pincode,
    Phone,
    1,
    GETDATE()
FROM ProfileAddr
WHERE Line1 IS NOT NULL;

PRINT CONCAT('Seeded default delivery addresses: ', @@ROWCOUNT);

/* Ensure every patient with addresses has exactly one default (oldest wins if none set). */
;WITH NeedDefault AS (
    SELECT d.PatientId
    FROM dbo.PatientDeliveryAddress d
    GROUP BY d.PatientId
    HAVING SUM(CASE WHEN d.IsDefault = 1 THEN 1 ELSE 0 END) = 0
),
Pick AS (
    SELECT d.DeliveryAddressId,
           ROW_NUMBER() OVER (PARTITION BY d.PatientId ORDER BY d.DeliveryAddressId ASC) AS rn
    FROM dbo.PatientDeliveryAddress d
    INNER JOIN NeedDefault n ON n.PatientId = d.PatientId
)
UPDATE d
SET d.IsDefault = 1
FROM dbo.PatientDeliveryAddress d
INNER JOIN Pick p ON p.DeliveryAddressId = d.DeliveryAddressId AND p.rn = 1;

PRINT CONCAT('Marked missing defaults: ', @@ROWCOUNT);
GO

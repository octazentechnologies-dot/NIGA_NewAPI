/*
  Extend SaaS subscription for Gourav_Dev_Doctor to 2027-12-31.

  Already applied on:
    - HomeoCentrum_Dev        (103.196.187.99)   — UPDATED PackageDetailId 5028 / DoctorId 2014
    - HomeoCentrum_Stage      (103.196.187.99)   — INSERTED PackageDetailId 6024 / DoctorId 2013
    - HomeoCentrum_Production (100.124.149.15)   — UPDATED PackageDetailId 6020 / DoctorId 2009
      (Azure Data Studio “(local)” on that machine; sa password provided by owner)
*/
SET NOCOUNT ON;
USE HomeoCentrum_Production;  -- change if needed
GO

DECLARE @UserName nvarchar(100) = N'Gourav_Dev_Doctor';
DECLARE @Expiry datetime = '2027-12-31T23:59:59';
DECLARE @UserId bigint = (
  SELECT TOP 1 UserId FROM dbo.UserMaster
  WHERE UserName = @UserName AND ISNULL(DeleteStatus, 0) = 0
);
IF @UserId IS NULL
BEGIN
  RAISERROR('UserMaster missing for Gourav_Dev_Doctor', 16, 1);
  RETURN;
END

DECLARE @DoctorId int = (
  SELECT TOP 1 DoctorID FROM dbo.Doctor
  WHERE UserId = CAST(@UserId AS INT) AND ISNULL(DeleteStatus, 0) = 0
);
IF @DoctorId IS NULL
BEGIN
  RAISERROR('Doctor row missing for Gourav_Dev_Doctor', 16, 1);
  RETURN;
END

DECLARE @PackageId int = (
  SELECT TOP 1 PackageId FROM dbo.PackageMaster
  WHERE ISNULL(DeleteStatus, 0) = 0
  ORDER BY ISNULL(ValidityInDays, 0) DESC, PackageId
);
IF @PackageId IS NULL SET @PackageId = 1;

UPDATE dbo.PackageEntryDetails
SET ExpiryDate = @Expiry,
    IsActive = 1,
    ActivationDate = ISNULL(ActivationDate, GETDATE())
WHERE DoctorId = @DoctorId;

IF @@ROWCOUNT = 0
BEGIN
  INSERT INTO dbo.PackageEntryDetails (
    PackageId, DoctorId, ActivationDate, ExpiryDate,
    OrderId, TransactionId, PaymentId, IsActive, CreatedBy, CreatedDate
  )
  VALUES (
    @PackageId, @DoctorId, GETDATE(), @Expiry,
    N'MANUAL-EXTEND-2027', N'MANUAL', N'MANUAL', 1, CAST(@UserId AS INT), GETDATE()
  );
  PRINT 'INSERTED PackageEntryDetails for Gourav_Dev_Doctor';
END
ELSE
  PRINT 'UPDATED PackageEntryDetails for Gourav_Dev_Doctor';

SELECT DB_NAME() AS DbName, @UserName AS UserName, @DoctorId AS DoctorId,
       PackageDetailId, PackageId, ActivationDate, ExpiryDate, IsActive
FROM dbo.PackageEntryDetails
WHERE DoctorId = @DoctorId
ORDER BY ExpiryDate DESC;
GO

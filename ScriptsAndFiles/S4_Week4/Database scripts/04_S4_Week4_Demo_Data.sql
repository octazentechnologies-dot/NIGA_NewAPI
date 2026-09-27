/*
S4 Week 4 — demo rows for collect, GST, trust queue, fees, payees.
Idempotent. Does not change an already Verified Tufan doctor.
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @DoctorId INT = (
    SELECT TOP 1 DoctorId FROM dbo.Doctor
    WHERE ISNULL(DeleteStatus, 0) = 0
    ORDER BY CASE WHEN FirstName LIKE N'%Tufan%' OR LastName LIKE N'%Tufan%' THEN 0 ELSE 1 END, DoctorId
);
DECLARE @UserId BIGINT = (SELECT TOP 1 UserId FROM dbo.Doctor WHERE DoctorId = @DoctorId);
DECLARE @PatientId INT = (
    SELECT TOP 1 PatientId FROM dbo.Patient
    WHERE ISNULL(DeleteStatus, 0) = 0
    ORDER BY PatientId DESC
);

IF @DoctorId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.ConsultFeeConfig WHERE DoctorId = @DoctorId)
    INSERT INTO dbo.ConsultFeeConfig (DoctorId, InClinicFee, TeleFee, InstantSurcharge, Currency, PayAtClinicEnabled, EffectiveFrom, CreatedBy, CreatedAt)
    VALUES (@DoctorId, 500, 400, 100, 'INR', 1, CAST(GETDATE() AS date), @UserId, GETDATE());

IF OBJECT_ID(N'dbo.TaxConfig', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.TaxConfig)
    INSERT INTO dbo.TaxConfig (GstRate, TreatmentExempt) VALUES (0, 1);

IF @DoctorId IS NOT NULL AND @PatientId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.PatientAppointment
        WHERE DoctorId = @DoctorId AND ISNULL(PaymentStatus, N'UNPAID') <> N'PAID'
          AND ISNULL(DeleteStatus, 0) = 0
          AND ISNULL(Status, N'') <> N'CANCELLED'
          AND AppointmentDate >= CAST(GETDATE() AS date)
   )
    INSERT INTO dbo.PatientAppointment
        (PatientId, AppointmentDate, AppointmentTime, Status, DeleteStatus, UserId, DoctorId,
         VisitType, ConsultMode, PaymentStatus, IsTele, PayAtClinicAllowed, BookingChannel)
    VALUES
        (@PatientId, CAST(DATEADD(DAY, 1, GETDATE()) AS date), '10:00:00', N'WAITING', 0, @UserId, @DoctorId,
         N'FollowUp', N'InClinic', N'UNPAID', 0, 1, N'Assisted');

IF OBJECT_ID(N'dbo.Payee', N'U') IS NOT NULL AND @DoctorId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Payee WHERE DoctorId = @DoctorId)
    INSERT INTO dbo.Payee (PayeeType, DoctorId, AccountName, BankAccount, Ifsc, Pan, KycStatus, UpdatedAt)
    VALUES (N'DOCTOR', @DoctorId, N'Tufan clinic', N'0000000000', N'SBIN0000000', N'ABCDE1234F', N'PENDING', GETDATE());

DECLARE @OtherDoctor INT = (
    SELECT TOP 1 DoctorId FROM dbo.Doctor
    WHERE ISNULL(DeleteStatus, 0) = 0
      AND DoctorId <> @DoctorId
      AND ISNULL(VerificationStatus, N'') IN (N'', N'Pending', N'Unverified')
    ORDER BY DoctorId
);
IF @OtherDoctor IS NOT NULL
    UPDATE dbo.Doctor SET VerificationStatus = N'Pending' WHERE DoctorId = @OtherDoctor AND ISNULL(VerificationStatus, N'') IN (N'', N'Unverified');

PRINT CONCAT('04_S4_Week4_Demo_Data.sql doctor=', ISNULL(CONVERT(varchar(20), @DoctorId), 'none'), ' patient=', ISNULL(CONVERT(varchar(20), @PatientId), 'none'));
GO

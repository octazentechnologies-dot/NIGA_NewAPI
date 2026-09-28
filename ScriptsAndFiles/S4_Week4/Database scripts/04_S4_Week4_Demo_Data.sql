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
DECLARE @PatientUserId BIGINT = (
    SELECT TOP 1 UserId FROM dbo.UserMaster WHERE UserName = N'Tufan_Patient' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @PatientId INT = (
    SELECT TOP 1 p.PatientId
    FROM dbo.Patient p
    INNER JOIN dbo.PatientUserMap m ON m.PatientId = p.PatientId
    WHERE m.UserId = @PatientUserId AND ISNULL(p.DeleteStatus, 0) = 0 AND ISNULL(m.DeleteStatus, 0) = 0
    ORDER BY CASE WHEN ISNULL(m.IsPrimary, 1) = 1 THEN 0 ELSE 1 END, p.PatientId DESC
);
IF @PatientId IS NULL
    SET @PatientId = (SELECT TOP 1 PatientId FROM dbo.Patient WHERE ISNULL(DeleteStatus, 0) = 0 ORDER BY PatientId DESC);

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

DECLARE @VisitId INT = (
    SELECT TOP 1 PatientAppId FROM dbo.PatientAppointment
    WHERE PatientId = @PatientId AND ISNULL(DeleteStatus, 0) = 0
    ORDER BY AppointmentDate DESC, PatientAppId DESC
);

IF @DoctorId IS NOT NULL AND @PatientId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.PatientAppointment
        WHERE PatientId = @PatientId AND AppointmentDate < CAST(GETDATE() AS date) AND ISNULL(DeleteStatus, 0) = 0
   )
BEGIN
    INSERT INTO dbo.PatientAppointment
        (PatientId, AppointmentDate, AppointmentTime, Status, DeleteStatus, UserId, DoctorId,
         VisitType, ConsultMode, PaymentStatus, IsTele, PayAtClinicAllowed, BookingChannel)
    VALUES
        (@PatientId, CAST(DATEADD(DAY, -12, GETDATE()) AS date), '11:00:00', N'COMPLETED', 0, @UserId, @DoctorId,
         N'First', N'InClinic', N'PAID', 0, 1, N'Assisted');
    SET @VisitId = SCOPE_IDENTITY();
END

IF OBJECT_ID(N'dbo.ErxSnapshot', N'U') IS NOT NULL AND @VisitId IS NOT NULL AND @PatientId IS NOT NULL AND @DoctorId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.ErxSnapshot WHERE PatientId = @PatientId AND Status = N'SIGNED')
    INSERT INTO dbo.ErxSnapshot (PatientAppId, PatientId, DoctorId, Status, SignedAt, SignedBy)
    VALUES (@VisitId, @PatientId, @DoctorId, N'SIGNED', DATEADD(DAY, -12, GETDATE()), @UserId);

IF OBJECT_ID(N'dbo.PharmacyPartner', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.PharmacyPartner WHERE Status <> N'DELETED')
    INSERT INTO dbo.PharmacyPartner (Name, Mobile, Area, Status, CreatedAt)
    VALUES (N'Demo HomeoMeds', N'7768046064', N'Pune', N'ACTIVE', GETDATE());

IF OBJECT_ID(N'dbo.SymptomDiary', N'U') IS NOT NULL AND @PatientId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SymptomDiary WHERE PatientId = @PatientId AND ISNULL(DeleteStatus, 0) = 0)
BEGIN
    INSERT INTO dbo.SymptomDiary (PatientId, EntryDate, Severity, Note, DeleteStatus, CreatedAt)
    VALUES
        (@PatientId, CAST(DATEADD(DAY, -10, GETDATE()) AS date), 6, N'Demo: cough and restlessness after evening tea.', 0, GETDATE()),
        (@PatientId, CAST(DATEADD(DAY, -5, GETDATE()) AS date), 4, N'Demo: better sleep, mild throat irritation.', 0, GETDATE()),
        (@PatientId, CAST(GETDATE() AS date), 3, N'Demo: energy improving; continue prescribed routine.', 0, GETDATE());
END

IF OBJECT_ID(N'dbo.FollowUpPlan', N'U') IS NOT NULL AND @VisitId IS NOT NULL AND @PatientId IS NOT NULL AND @DoctorId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.FollowUpPlan WHERE PatientId = @PatientId)
BEGIN
    INSERT INTO dbo.FollowUpPlan (PatientAppId, PatientId, DoctorId, Note, CreatedAt)
    VALUES (@VisitId, @PatientId, @DoctorId, N'Demo follow-up from S4 seed.', GETDATE());
    DECLARE @PlanId INT = SCOPE_IDENTITY();
    INSERT INTO dbo.FollowUpTask (FollowUpPlanId, Title, DueDate, Status)
    VALUES
        (@PlanId, N'Review cough after 7 days', CAST(DATEADD(DAY, 7, GETDATE()) AS date), N'OPEN'),
        (@PlanId, N'Bring previous reports', CAST(DATEADD(DAY, 3, GETDATE()) AS date), N'OPEN');
END

IF OBJECT_ID(N'dbo.PatientHealthBasics', N'U') IS NOT NULL AND @PatientId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.PatientHealthBasics WHERE PatientId = @PatientId)
    INSERT INTO dbo.PatientHealthBasics (PatientId, BloodGroup, Allergies, ChronicConditions, EmergencyContactName, EmergencyContactMobile, UpdatedAt)
    VALUES (@PatientId, N'B+', N'None recorded', N'Demo cough / follow-up case', N'Tufan Caregiver', N'7768046064', GETDATE());

PRINT CONCAT('04_S4_Week4_Demo_Data.sql doctor=', ISNULL(CONVERT(varchar(20), @DoctorId), 'none'), ' patient=', ISNULL(CONVERT(varchar(20), @PatientId), 'none'));
GO

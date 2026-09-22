/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 24_DEV_Seed_Tufan_Doctor_Clinic.sql
Purpose      : Make Tufan_Doctor a full clinic login like NIGA HOMEOPATHY:
               profile, KYC, week schedule, credential stub, cases, today's
               appointments (all dashboard statuses), notes, sample eRx.
               S3–S5 provisions on those rows: VisitType, ConsultMode,
               PaymentStatus, IsTele, ConsentPolicyVersion 2026.09.
Use          : HomeoCentrum_Dev after 15/19. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Doctor', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Patient', N'U') IS NULL
   OR OBJECT_ID(N'dbo.CaseEntryDetails', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
BEGIN
    RAISERROR('Required clinic tables missing.', 16, 1);
    RETURN;
END

DECLARE @DocUserId INT = (
    SELECT TOP 1 CAST(UserId AS INT)
    FROM dbo.UserMaster
    WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus, 0) = 0
);
DECLARE @DoctorId INT = (
    SELECT TOP 1 DoctorID
    FROM dbo.Doctor
    WHERE UserId = @DocUserId AND ISNULL(DeleteStatus, 0) = 0
);

IF @DocUserId IS NULL OR @DoctorId IS NULL
BEGIN
    RAISERROR('Tufan_Doctor UserMaster/Doctor missing. Run 15_DEV_Seed_Tufan_Role_Logins.sql first.', 16, 1);
    RETURN;
END

DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @Yesterday DATE = DATEADD(DAY, -1, @Today);
DECLARE @QualId INT = (
    SELECT TOP 1 QualificationID FROM dbo.QualificationMaster
    WHERE ISNULL(DeleteStatus, 0) = 0
    ORDER BY QualificationID
);
DECLARE @AsthmaId INT = (SELECT TOP 1 DiagnosisId FROM dbo.DiagnosisMaster WHERE DiagnosisName LIKE N'ASTHMA%' AND ISNULL(DeleteStatus, 0) = 0);
DECLARE @AcneId INT = (SELECT TOP 1 DiagnosisId FROM dbo.DiagnosisMaster WHERE DiagnosisName LIKE N'ACNE%' AND ISNULL(DeleteStatus, 0) = 0);
DECLARE @AlopeciaId INT = (SELECT TOP 1 DiagnosisId FROM dbo.DiagnosisMaster WHERE DiagnosisName LIKE N'ALOPECIA%' AND ISNULL(DeleteStatus, 0) = 0);
DECLARE @RemedyId INT = (SELECT TOP 1 RemedyId FROM dbo.RemedyMaster WHERE ISNULL(DeleteStatus, 0) = 0 ORDER BY RemedyId);
DECLARE @TufanPatientId INT = (
    SELECT TOP 1 p.PatientID
    FROM dbo.Patient p
    INNER JOIN dbo.PatientUserMap m ON m.PatientId = p.PatientID AND ISNULL(m.DeleteStatus, 0) = 0
    INNER JOIN dbo.UserMaster u ON u.UserId = m.UserId
    WHERE u.UserName = N'Tufan_Patient' AND ISNULL(p.DeleteStatus, 0) = 0
);
DECLARE @d INT;
DECLARE @Sched DATE;
DECLARE @n INT;

BEGIN TRAN;

-- ---------------------------------------------------------------------------
-- Doctor profile parity (fees already on 15; fill university / hours / cert)
-- ---------------------------------------------------------------------------
UPDATE dbo.Doctor
SET ClinicName = ISNULL(NULLIF(LTRIM(RTRIM(ClinicName)), N''), N'Tufan Homeopathy Clinic'),
    ConsultFeeInClinic = ISNULL(ConsultFeeInClinic, 550),
    ConsultFeeTele = ISNULL(ConsultFeeTele, 420),
    WorkingHoursNote = ISNULL(NULLIF(LTRIM(RTRIM(WorkingHoursNote)), N''), N'Mon-Sat 10:00-18:00'),
    PassingUniversity = ISNULL(NULLIF(LTRIM(RTRIM(PassingUniversity)), N''), N'MUHS Nashik'),
    PassingCertNo = ISNULL(NULLIF(LTRIM(RTRIM(PassingCertNo)), N''), N'DEV-TUFAN-DOC-1010'),
    CasePaperValidity = ISNULL(CasePaperValidity, 365),
    QualificationID = COALESCE(QualificationID, @QualId),
    City = ISNULL(NULLIF(LTRIM(RTRIM(City)), N''), N'Pune'),
    PermanantAddress = ISNULL(NULLIF(LTRIM(RTRIM(PermanantAddress)), N''), N'Tufan Homeopathy Clinic, Pune'),
    CountryId = ISNULL(CountryId, 78),
    StateId = ISNULL(StateId, 14),
    IsOnline = 1,
    DirectoryVisible = 1,
    VerificationStatus = N'Verified',
    PracticeActivated = 1,
    DeleteStatus = 0,
    ChangedBy = N'TUFAN-DOC-CLINIC',
    ChangedDate = GETDATE()
WHERE DoctorID = @DoctorId;
PRINT CONCAT('UPDATED Doctor profile DoctorId=', @DoctorId);

IF OBJECT_ID(N'dbo.DoctorPayeeKyc', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.DoctorPayeeKyc
        WHERE DoctorId = @DoctorId AND ISNULL(DeleteStatus, 0) = 0
   )
BEGIN
    INSERT INTO dbo.DoctorPayeeKyc (DoctorId, AccountHolder, BankName, AccountNumber, Ifsc, Pan, CreatedAt, DeleteStatus)
    VALUES (@DoctorId, N'Tufan Doctor', N'S2-TESTED Bank', N'XXXX00001010', N'HDFC0001234', N'ABCDE1234F', GETUTCDATE(), 0);
    PRINT 'INSERTED DoctorPayeeKyc for Tufan_Doctor';
END

IF OBJECT_ID(N'dbo.DoctorDailySchedule', N'U') IS NOT NULL
BEGIN
    SET DATEFIRST 7; -- Sunday = 1
    SET @d = 0;
    WHILE @d < 14
    BEGIN
        SET @Sched = DATEADD(DAY, @d, @Today);
        IF DATEPART(WEEKDAY, @Sched) <> 1
           AND NOT EXISTS (
                SELECT 1 FROM dbo.DoctorDailySchedule
                WHERE DoctorId = @DoctorId AND ScheduleDate = @Sched
           )
        BEGIN
            INSERT INTO dbo.DoctorDailySchedule (
                DoctorId, ScheduleDate, SlotIntervalMinutes, WorkStartTime, WorkEndTime, CreatedByUserId, CreatedAt
            )
            VALUES (@DoctorId, @Sched, 15, CAST('10:00' AS TIME), CAST('18:00' AS TIME), @DocUserId, GETUTCDATE());
        END
        SET @d += 1;
    END
    PRINT 'UPSERTED DoctorDailySchedule next 14 days (skip Sunday) for Tufan_Doctor';
END

IF OBJECT_ID(N'dbo.DoctorVerification', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.DoctorVerification
        WHERE DoctorId = @DoctorId AND ISNULL(DeleteStatus, 0) = 0
   )
BEGIN
    INSERT INTO dbo.DoctorVerification (DoctorId, Status, EnteredDate, DeleteStatus)
    VALUES (@DoctorId, N'Approved', GETUTCDATE(), 0);
    PRINT 'INSERTED DoctorVerification Approved';
END

DECLARE @VerifyId INT = (
    SELECT TOP 1 DoctorVerificationId FROM dbo.DoctorVerification
    WHERE DoctorId = @DoctorId AND ISNULL(DeleteStatus, 0) = 0
);

IF OBJECT_ID(N'dbo.DoctorCredentialDocument', N'U') IS NOT NULL
   AND @VerifyId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.DoctorCredentialDocument
        WHERE DoctorId = @DoctorId AND ISNULL(DeleteStatus, 0) = 0
   )
BEGIN
    INSERT INTO dbo.DoctorCredentialDocument (
        DoctorId, DoctorVerificationId, DocumentType, FileName, FilePath, ContentType, EnteredDate, DeleteStatus
    )
    VALUES (
        @DoctorId, @VerifyId, N'RegistrationCertificate',
        N'tufan-doctor-registration.pdf',
        N'/dev/credentials/tufan-doctor-registration.pdf',
        N'application/pdf', GETUTCDATE(), 0
    );
    PRINT 'INSERTED DoctorCredentialDocument stub (S3 TRU provision)';
END

-- Active SaaS plan: same window as NIGA HOMEOPATHY, on DoctorId AND UserId
-- (AuthService historically looked up PackageEntryDetails.DoctorId == UserMaster.UserId).
IF OBJECT_ID(N'dbo.PackageEntryDetails', N'U') IS NOT NULL
BEGIN
    DECLARE @NigaDoctorId INT = (
        SELECT TOP 1 d.DoctorID
        FROM dbo.Doctor d
        INNER JOIN dbo.UserMaster u ON u.UserId = d.UserId
        WHERE u.UserName = N'NIGA HOMEOPATHY' AND ISNULL(d.DeleteStatus, 0) = 0
    );
    IF @NigaDoctorId IS NULL SET @NigaDoctorId = 3;

    DECLARE @NigaPkg INT, @NigaAct DATETIME, @NigaExp DATETIME;
    SELECT TOP 1
        @NigaPkg = PackageId,
        @NigaAct = ActivationDate,
        @NigaExp = ExpiryDate
    FROM dbo.PackageEntryDetails
    WHERE DoctorId = @NigaDoctorId AND ISNULL(IsActive, 0) = 1 AND ExpiryDate > GETUTCDATE()
    ORDER BY ExpiryDate DESC;

    IF @NigaPkg IS NULL
        SET @NigaPkg = (
            SELECT TOP 1 PackageId FROM dbo.PackageMaster
            WHERE ISNULL(DeleteStatus, 0) = 0
            ORDER BY PackageId
        );
    IF @NigaAct IS NULL SET @NigaAct = GETDATE();
    IF @NigaExp IS NULL SET @NigaExp = DATEADD(YEAR, 1, GETDATE());

    DECLARE @MapId INT = @DoctorId;
    IF @MapId IS NOT NULL
    BEGIN
        IF EXISTS (
            SELECT 1 FROM dbo.PackageEntryDetails
            WHERE DoctorId = @MapId AND ISNULL(IsActive, 0) = 1
        )
        BEGIN
            UPDATE dbo.PackageEntryDetails
            SET ExpiryDate = CASE WHEN ExpiryDate < @NigaExp THEN @NigaExp ELSE ExpiryDate END,
                IsActive = 1
            WHERE DoctorId = @MapId AND ISNULL(IsActive, 0) = 1;
            PRINT CONCAT('UPDATED PackageEntryDetails for DoctorId=', @MapId);
        END
        ELSE
        BEGIN
            INSERT INTO dbo.PackageEntryDetails (
                PackageId, DoctorId, ActivationDate, ExpiryDate, OrderId, TransactionId, PaymentId,
                IsActive, CreatedBy, CreatedDate
            )
            VALUES (
                @NigaPkg, @MapId, @NigaAct, @NigaExp,
                N'DEV-TUFAN-DOC', N'DEV-TUFAN-DOC', N'DEV-TUFAN-DOC',
                1, @DocUserId, GETDATE()
            );
            PRINT CONCAT('INSERTED PackageEntryDetails for DoctorId=', @MapId);
        END
    END

    IF COL_LENGTH(N'dbo.Doctor', N'PackageId') IS NOT NULL
        UPDATE dbo.Doctor SET PackageId = @NigaPkg WHERE DoctorID = @DoctorId AND ISNULL(DeleteStatus, 0) = 0;
END

-- ---------------------------------------------------------------------------
-- Clinic patients (unique mobiles; Tufan_Patient 3046 reused)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'tempdb..#ClinicPts') IS NOT NULL DROP TABLE #ClinicPts;
CREATE TABLE #ClinicPts (
    SortNo INT NOT NULL,
    PatientName NVARCHAR(200) NOT NULL,
    MobileNo NVARCHAR(20) NOT NULL,
    Gender INT NOT NULL,
    Dob DATE NOT NULL,
    Email NVARCHAR(100) NULL,
    ExistingPatientId INT NULL,
    PatientId INT NULL,
    CaseId INT NULL,
    ApptStatus NVARCHAR(50) NOT NULL,
    ApptDate DATE NOT NULL,
    ApptTime TIME NOT NULL,
    VisitType NVARCHAR(50) NOT NULL,
    ConsultMode NVARCHAR(50) NOT NULL,
    PaymentStatus NVARCHAR(30) NOT NULL,
    IsTele BIT NOT NULL,
    Token NVARCHAR(64) NOT NULL,
    Complaint NVARCHAR(200) NOT NULL,
    DiagnosisId INT NULL
);

INSERT INTO #ClinicPts (
    SortNo, PatientName, MobileNo, Gender, Dob, Email, ExistingPatientId,
    ApptStatus, ApptDate, ApptTime, VisitType, ConsultMode, PaymentStatus, IsTele,
    Token, Complaint, DiagnosisId
) VALUES
    (1, N'Tufan Patient',          N'7768046064', 0, '1990-01-01', N'tufanpowar001@gmail.com', @TufanPatientId,
     N'WAITING',     @Today,     '10:00', N'First',    N'InClinic', N'UNPAID',      0, N'TUFANDOC01', N'Headache since 2 weeks', @AsthmaId),
    (2, N'Clinic Demo Asha',       N'9000000301', 1, '1988-03-12', N'asha.clinic@homeocentrum.dev', NULL,
     N'WALK-IN',     @Today,     '10:15', N'First',    N'InClinic', N'PayAtClinic', 0, N'TUFANDOC02', N'Cough with wheeze', @AsthmaId),
    (3, N'Clinic Demo Rohan',      N'9000000302', 0, '1992-07-21', N'rohan.clinic@homeocentrum.dev', NULL,
     N'REMAINING',   @Today,     '10:30', N'FollowUp', N'InClinic', N'Paid',        0, N'TUFANDOC03', N'Acne on face', @AcneId),
    (4, N'Clinic Demo Meera',      N'9000000303', 1, '1985-11-04', N'meera.clinic@homeocentrum.dev', NULL,
     N'E-CONSULT',   @Today,     '11:00', N'Tele',     N'Tele',     N'UNPAID',      1, N'TUFANDOC04', N'Hair fall', @AlopeciaId),
    (5, N'Clinic Demo Kabir',      N'9000000304', 0, '1979-01-18', N'kabir.clinic@homeocentrum.dev', NULL,
     N'NOT ARRIVED', @Today,     '11:30', N'InClinic', N'InClinic', N'PENDING',     0, N'TUFANDOC05', N'Seasonal allergy', @AsthmaId),
    (6, N'Clinic Demo Nisha',      N'9000000305', 1, '1995-09-09', N'nisha.clinic@homeocentrum.dev', NULL,
     N'COMPLETED',   @Today,     '09:30', N'FollowUp', N'InClinic', N'Paid',        0, N'TUFANDOC06', N'Acne follow-up', @AcneId),
    (7, N'Clinic Demo Vivek',      N'9000000306', 0, '1998-05-30', N'vivek.clinic@homeocentrum.dev', NULL,
     N'WAITING',     @Today,     '12:00', N'First',    N'InClinic', N'UNPAID',      0, N'TUFANDOC07', N'Breathlessness on exertion', @AsthmaId),
    (8, N'Clinic Demo Priya',      N'9000000307', 1, '1991-12-15', N'priya.clinic@homeocentrum.dev', NULL,
     N'COMPLETED',   @Yesterday, '16:00', N'FollowUp', N'InClinic', N'Paid',        0, N'TUFANDOC08', N'Chronic rhinitis', @AsthmaId);

UPDATE t
SET PatientId = t.ExistingPatientId
FROM #ClinicPts t
WHERE t.ExistingPatientId IS NOT NULL;

UPDATE t
SET PatientId = p.PatientID
FROM #ClinicPts t
INNER JOIN dbo.Patient p ON p.MobileNo = t.MobileNo AND ISNULL(p.DeleteStatus, 0) = 0
WHERE t.PatientId IS NULL AND t.ExistingPatientId IS NULL;

INSERT INTO dbo.Patient (
    PatientName, Address, StateId, CountryId, MobileNo, DateOfBirth, Gender, Email, Age,
    IsWhatsAppOptIn, DeleteStatus, EnteredBy, EnteredDate
)
SELECT
    t.PatientName,
    N'Tufan Homeopathy Clinic demo',
    14, 78,
    t.MobileNo,
    t.Dob,
    t.Gender,
    t.Email,
    DATEDIFF(YEAR, t.Dob, GETDATE()),
    0, 0, N'TUFAN-DOC-CLINIC', GETDATE()
FROM #ClinicPts t
WHERE t.PatientId IS NULL;

UPDATE t
SET PatientId = p.PatientID
FROM #ClinicPts t
INNER JOIN dbo.Patient p ON p.MobileNo = t.MobileNo AND ISNULL(p.DeleteStatus, 0) = 0
WHERE t.PatientId IS NULL;

SET @n = (SELECT COUNT(*) FROM #ClinicPts WHERE PatientId IS NOT NULL);
PRINT CONCAT('Clinic patients ready = ', @n);

-- Cases (DoctorId = Doctor.DoctorID; UserId = doctor UserMaster — matches getAllCases join)
UPDATE t
SET CaseId = c.CaseId
FROM #ClinicPts t
INNER JOIN dbo.CaseEntryDetails c
    ON c.PatientId = t.PatientId AND c.DoctorId = @DoctorId AND ISNULL(c.DeleteStatus, 0) = 0;

INSERT INTO dbo.CaseEntryDetails (
    PatientId, UserId, DoctorId, DateodFirstVisit, RefBy, EnteredBy, EnteredDate, DeleteStatus
)
SELECT
    t.PatientId, @DocUserId, @DoctorId, CAST(t.ApptDate AS DATETIME), N'self',
    N'TUFAN-DOC-CLINIC', GETDATE(), 0
FROM #ClinicPts t
WHERE t.PatientId IS NOT NULL AND t.CaseId IS NULL;

UPDATE t
SET CaseId = c.CaseId
FROM #ClinicPts t
INNER JOIN dbo.CaseEntryDetails c
    ON c.PatientId = t.PatientId AND c.DoctorId = @DoctorId AND ISNULL(c.DeleteStatus, 0) = 0
WHERE t.CaseId IS NULL;

IF OBJECT_ID(N'dbo.CaseEntryChiefComplaint', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.CaseEntryChiefComplaint (CaseId, ChiefComplaintName)
    SELECT t.CaseId, t.Complaint
    FROM #ClinicPts t
    WHERE t.CaseId IS NOT NULL
      AND NOT EXISTS (
            SELECT 1 FROM dbo.CaseEntryChiefComplaint cc
            WHERE cc.CaseId = t.CaseId AND cc.ChiefComplaintName = t.Complaint
      );
END

IF OBJECT_ID(N'dbo.CaseEntryDiagnosis', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.CaseEntryDiagnosis (CaseId, DiagnosisId)
    SELECT t.CaseId, t.DiagnosisId
    FROM #ClinicPts t
    WHERE t.CaseId IS NOT NULL AND t.DiagnosisId IS NOT NULL
      AND NOT EXISTS (
            SELECT 1 FROM dbo.CaseEntryDiagnosis d
            WHERE d.CaseId = t.CaseId AND d.DiagnosisId = t.DiagnosisId
      );
END

SET @n = (SELECT COUNT(*) FROM #ClinicPts WHERE CaseId IS NOT NULL);
PRINT CONCAT('Cases ready = ', @n);

-- Appointments (UserId = doctor UserMaster so dashboard buckets populate)
INSERT INTO dbo.PatientAppointment (
    PatientId, AppointmentDate, AppointmentTime, Status, DeleteStatus, UserId, DoctorId,
    BookingToken, VisitType, ConsultMode, PaymentStatus, IsTele, HoldExpiresAt, ConsentPolicyVersion
)
SELECT
    t.PatientId,
    CAST(t.ApptDate AS DATETIME),
    t.ApptTime,
    t.ApptStatus,
    0,
    @DocUserId,
    @DoctorId,
    t.Token,
    t.VisitType,
    t.ConsultMode,
    t.PaymentStatus,
    t.IsTele,
    NULL,
    N'2026.09'
FROM #ClinicPts t
WHERE t.PatientId IS NOT NULL
  AND NOT EXISTS (
        SELECT 1 FROM dbo.PatientAppointment a
        WHERE a.BookingToken = t.Token
  );

SET @n = (SELECT COUNT(*) FROM dbo.PatientAppointment WHERE BookingToken LIKE N'TUFANDOC%');
PRINT CONCAT('Appointments with TUFANDOC tokens = ', @n);

-- Keep the Dev clinic on "today" when the script is re-run after midnight.
UPDATE a
SET AppointmentDate = CASE
        WHEN t.ApptStatus = N'COMPLETED' AND t.Token = N'TUFANDOC08'
            THEN CAST(@Yesterday AS DATETIME)
        ELSE CAST(@Today AS DATETIME)
    END,
    AppointmentTime = t.ApptTime,
    Status = t.ApptStatus,
    VisitType = t.VisitType,
    ConsultMode = t.ConsultMode,
    PaymentStatus = t.PaymentStatus,
    IsTele = t.IsTele,
    ConsentPolicyVersion = N'2026.09',
    DeleteStatus = 0
FROM dbo.PatientAppointment a
INNER JOIN #ClinicPts t ON t.Token = a.BookingToken;


IF OBJECT_ID(N'dbo.AppointmentHistoryNote', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.AppointmentHistoryNote (AppointmentId, HistoryNote, DeletedStatus, CreatedBy, CreatedDate)
    SELECT a.PatientAppId,
           N'<p>Dev sample note for ' + t.PatientName + N'. Classic notes stay until Phase 10 eRx split (CLN-15.03).</p>',
           0, @DocUserId, GETDATE()
    FROM #ClinicPts t
    INNER JOIN dbo.PatientAppointment a ON a.BookingToken = t.Token
    WHERE t.ApptStatus = N'COMPLETED'
      AND NOT EXISTS (
            SELECT 1 FROM dbo.AppointmentHistoryNote n
            WHERE n.AppointmentId = a.PatientAppId AND ISNULL(n.DeletedStatus, 0) = 0
      );
END

IF OBJECT_ID(N'dbo.PrescriptionRemedyDetail', N'U') IS NOT NULL AND @RemedyId IS NOT NULL
BEGIN
    INSERT INTO dbo.PrescriptionRemedyDetail (AppointmentId, RemedyId, Description, Dose, DeletedStatus, CreatedDate)
    SELECT a.PatientAppId, @RemedyId, N'Dev sample eRx provision (full eRx is S5/Phase 10)', N'30C od x 7d', 0, GETDATE()
    FROM #ClinicPts t
    INNER JOIN dbo.PatientAppointment a ON a.BookingToken = t.Token
    WHERE t.ApptStatus = N'COMPLETED'
      AND NOT EXISTS (
            SELECT 1 FROM dbo.PrescriptionRemedyDetail r
            WHERE r.AppointmentId = a.PatientAppId AND ISNULL(r.DeletedStatus, 0) = 0
      );
END

COMMIT TRAN;

SELECT
    @DoctorId AS DoctorId,
    @DocUserId AS DoctorUserId,
    (SELECT COUNT(*) FROM dbo.CaseEntryDetails WHERE DoctorId = @DoctorId AND ISNULL(DeleteStatus, 0) = 0) AS Cases,
    (SELECT COUNT(*) FROM dbo.PatientAppointment WHERE DoctorId = @DoctorId AND ISNULL(DeleteStatus, 0) = 0) AS Appointments,
    (SELECT COUNT(*) FROM dbo.PatientAppointment WHERE DoctorId = @DoctorId AND CAST(AppointmentDate AS date) = @Today AND ISNULL(DeleteStatus, 0) = 0) AS AppointmentsToday,
    (SELECT COUNT(*) FROM dbo.DoctorDailySchedule WHERE DoctorId = @DoctorId) AS ScheduleDays,
    (SELECT COUNT(*) FROM dbo.DoctorPayeeKyc WHERE DoctorId = @DoctorId AND ISNULL(DeleteStatus, 0) = 0) AS KycRows;

PRINT '24_DEV_Seed_Tufan_Doctor_Clinic.sql completed.';
PRINT 'Login Tufan_Doctor / 123456 — dashboard should show today buckets + cases for Excel/PDF export.';
GO

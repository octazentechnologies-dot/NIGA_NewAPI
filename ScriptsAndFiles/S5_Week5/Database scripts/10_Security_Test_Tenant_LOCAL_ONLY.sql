/*
  LOCAL / TEST DATABASES ONLY. Never run on production.
  Creates a second, isolated tenant used by the cross-tenant (IDOR) tests:
    Tufan_Doctor2   (RoleId 3) + Doctor row
    Tufan_Patient2  (RoleId 5) mapped to a patient owned by Tufan_Doctor2
    Tufan_Reception2 reception staff of Tufan_Doctor2
  Passwords are copied from Tufan_Doctor so all test logins share the same dev password.
  Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRAN;

DECLARE @hash NVARCHAR(500) = (SELECT UserPassword FROM dbo.UserMaster WHERE UserName = 'Tufan_Doctor');
DECLARE @receptionPassword NVARCHAR(200) = (SELECT TOP 1 [Password] FROM dbo.DoctorReceptionStaff WHERE UserID = 'Tufan_Reception');
IF @hash IS NULL THROW 50001, 'Tufan_Doctor not found; seed the base test users first.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = 'Tufan_Doctor2')
    INSERT dbo.UserMaster (UserName, UserPassword, UserStatus, DeleteStatus, IsUserActivated, RoleId, FirstName, LastName, EmailId, MobileNo, EnteredBy, EnteredDate)
    VALUES ('Tufan_Doctor2', @hash, 1, 0, 1, 3, 'Tufan', 'DoctorTwo', 'tufan.doctor2@example.invalid', '9000000002', 'SECURITY-TEST', GETDATE());
DECLARE @doctor2User BIGINT = (SELECT UserId FROM dbo.UserMaster WHERE UserName = 'Tufan_Doctor2');

IF NOT EXISTS (SELECT 1 FROM dbo.Doctor WHERE UserId = @doctor2User)
    INSERT dbo.Doctor (FirstName, LastName, MobileNo, EmailId, DeleteStatus, UserId, VerificationStatus, PracticeActivated, ClinicName, EnteredBy, EnteredDate)
    VALUES ('Tufan', 'DoctorTwo', '9000000002', 'tufan.doctor2@example.invalid', 0, @doctor2User, 'Verified', 1, 'Security Test Clinic', 'SECURITY-TEST', GETDATE());
DECLARE @doctor2 INT = (SELECT DoctorID FROM dbo.Doctor WHERE UserId = @doctor2User);

IF NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = 'Tufan_Patient2')
    INSERT dbo.UserMaster (UserName, UserPassword, UserStatus, DeleteStatus, IsUserActivated, RoleId, FirstName, LastName, EmailId, MobileNo, EnteredBy, EnteredDate)
    VALUES ('Tufan_Patient2', @hash, 1, 0, 1, 5, 'Tufan', 'PatientTwo', 'tufan.patient2@example.invalid', '9000000003', 'SECURITY-TEST', GETDATE());
DECLARE @patient2User BIGINT = (SELECT UserId FROM dbo.UserMaster WHERE UserName = 'Tufan_Patient2');

DECLARE @patient2 INT = (SELECT TOP 1 PatientId FROM dbo.PatientUserMap WHERE UserId = @patient2User AND DeleteStatus = 0);
IF @patient2 IS NULL
BEGIN
    INSERT dbo.Patient (PatientName, MobileNo, Gender, DeleteStatus, Email, EnteredBy, EnteredDate)
    VALUES ('Tufan PatientTwo', '9000000003', 1, 0, 'tufan.patient2@example.invalid', 'SECURITY-TEST', GETDATE());
    SET @patient2 = SCOPE_IDENTITY();
    INSERT dbo.PatientUserMap (UserId, PatientId, IsPrimary, DeleteStatus, EnteredBy, EnteredDate)
    VALUES (@patient2User, @patient2, 1, 0, 'SECURITY-TEST', GETDATE());
END

IF NOT EXISTS (SELECT 1 FROM dbo.CaseEntryDetails WHERE PatientId = @patient2 AND DoctorId = @doctor2)
    INSERT dbo.CaseEntryDetails (PatientId, UserId, DoctorId, DateodFirstVisit, EnteredBy, EnteredDate, DeleteStatus)
    VALUES (@patient2, @doctor2User, @doctor2, GETDATE(), 'SECURITY-TEST', GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM dbo.DoctorReceptionStaff WHERE UserID = 'Tufan_Reception2')
    INSERT dbo.DoctorReceptionStaff (DoctorID, UserID, [Password], FullName, ContactNumber, EnteredDate, DeleteStatus, IsActive)
    VALUES (@doctor2, 'Tufan_Reception2', ISNULL(@receptionPassword, @hash), 'Tufan ReceptionTwo', '9000000004', GETDATE(), 0, 1);

COMMIT;

SELECT @doctor2User AS Doctor2UserId, @doctor2 AS Doctor2Id, @patient2User AS Patient2UserId, @patient2 AS Patient2Id,
       (SELECT TOP 1 CaseId FROM dbo.CaseEntryDetails WHERE PatientId = @patient2 AND DoctorId = @doctor2) AS Patient2CaseId;

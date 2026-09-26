/*
================================================================================
Author       : Tufan Powar
Created      : 21-09-2026
Script       : 23_DEV_Rename_Tufan_Doctor.sql
Purpose      : Rename clinic doctor login Tufan_Doctore → Tufan_Doctor.
               Unlink the extra Patient created for Tufan_Caregiver so Family
               resolves through CaregiverAuthorization to Tufan_Patient.
Use          : HomeoCentrum_Dev only. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- 1) Doctor login spelling
IF EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Doctore' AND ISNULL(DeleteStatus,0)=0)
   AND NOT EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0)
BEGIN
    UPDATE dbo.UserMaster
    SET UserName = N'Tufan_Doctor',
        LastName = N'Doctor',
        EmailId = N'tufanpowar001@gmail.com',
        ChangedBy = N'TUFAN-TEAM',
        ChangedDate = GETUTCDATE()
    WHERE UserName = N'Tufan_Doctore' AND ISNULL(DeleteStatus,0)=0;
    PRINT 'RENAMED UserMaster Tufan_Doctore → Tufan_Doctor';
END
ELSE IF EXISTS (SELECT 1 FROM dbo.UserMaster WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0)
BEGIN
    UPDATE dbo.UserMaster
    SET LastName = N'Doctor',
        EmailId = N'tufanpowar001@gmail.com'
    WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0
      AND (LastName <> N'Doctor' OR EmailId <> N'tufanpowar001@gmail.com');
    PRINT 'UserMaster Tufan_Doctor already present';
END
ELSE
    PRINT 'SKIP: neither Tufan_Doctore nor Tufan_Doctor found';

DECLARE @DocUserId INT = (
    SELECT TOP 1 CAST(UserId AS INT)
    FROM dbo.UserMaster
    WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus,0)=0
);

IF @DocUserId IS NOT NULL AND OBJECT_ID(N'dbo.Doctor', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.Doctor
    SET LastName = N'Doctor',
        EmailId = N'tufanpowar001@gmail.com'
    WHERE UserId = @DocUserId AND ISNULL(DeleteStatus,0)=0;
    PRINT CONCAT('UPDATED Doctor LastName/Email for UserId=', @DocUserId);
END

-- 2) Caregiver should not own a separate Patient. Keep CaregiverAuthorization → Tufan_Patient.
DECLARE @CgUserId BIGINT = (
    SELECT TOP 1 UserId FROM dbo.UserMaster
    WHERE UserName = N'Tufan_Caregiver' AND ISNULL(DeleteStatus,0)=0
);
DECLARE @PatientUserId BIGINT = (
    SELECT TOP 1 UserId FROM dbo.UserMaster
    WHERE UserName = N'Tufan_Patient' AND ISNULL(DeleteStatus,0)=0
);
DECLARE @PatientId INT = (
    SELECT TOP 1 PatientId FROM dbo.PatientUserMap
    WHERE UserId = @PatientUserId AND ISNULL(DeleteStatus,0)=0 AND IsPrimary = 1
);

IF @CgUserId IS NOT NULL AND @PatientId IS NOT NULL
BEGIN
    UPDATE dbo.PatientUserMap
    SET DeleteStatus = 1
    WHERE UserId = @CgUserId
      AND ISNULL(DeleteStatus,0)=0
      AND PatientId <> @PatientId;

    PRINT CONCAT('UNLINKED extra PatientUserMap rows for Tufan_Caregiver (kept acting-for PatientId=', @PatientId, ')');
END

-- Soft-delete orphan Patient rows created as "Tufan Caregiver" when they are not the acting-for patient.
UPDATE p
SET p.DeleteStatus = 1,
    p.ChangedBy = N'TUFAN-TEAM',
    p.ChangedDate = GETUTCDATE()
FROM dbo.Patient p
WHERE p.PatientName = N'Tufan Caregiver'
  AND ISNULL(p.DeleteStatus,0)=0
  AND (@PatientId IS NULL OR p.PatientId <> @PatientId)
  AND NOT EXISTS (
        SELECT 1 FROM dbo.PatientUserMap m
        WHERE m.PatientId = p.PatientId AND ISNULL(m.DeleteStatus,0)=0
    )
  AND NOT EXISTS (
        SELECT 1 FROM dbo.PatientFamilyMember f
        WHERE f.MemberPatientId = p.PatientId AND ISNULL(f.DeleteStatus,0)=0
    );

PRINT '--- verify ---';
SELECT UserId, UserName, FirstName, LastName, EmailId, RoleId
FROM dbo.UserMaster
WHERE UserName IN (N'Tufan_Doctor', N'Tufan_Doctore', N'Tufan_Caregiver', N'Tufan_Patient');

SELECT d.DoctorID, d.UserId, d.FirstName, d.LastName, d.EmailId
FROM dbo.Doctor d
WHERE d.UserId = @DocUserId;

SELECT m.PatientUserMapId, m.UserId, m.PatientId, m.IsPrimary, m.DeleteStatus
FROM dbo.PatientUserMap m
WHERE m.UserId IN (@CgUserId, @PatientUserId);

SELECT c.CaregiverAuthorizationId, c.PatientId, c.CaregiverUserId, c.Scope, c.RevokedAt, c.DeleteStatus
FROM dbo.CaregiverAuthorization c
WHERE c.CaregiverUserId = @CgUserId;

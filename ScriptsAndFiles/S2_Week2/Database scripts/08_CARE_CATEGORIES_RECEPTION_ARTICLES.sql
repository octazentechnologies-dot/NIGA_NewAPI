/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 08_CARE_CATEGORIES_RECEPTION_ARTICLES.sql
Purpose      : Fill CareCategories (HumanSystemMaster), seed one Dev reception
               login so DoctorOnly 403 can be proven, and one S2-TESTED article
               body for GET /api/Public/Articles/{id}.
Use          : Dev/test only. Run after 01–07 on HomeoCentrum_Dev.
Marker       : S2-TESTED
Idempotent   : Yes. Skips names / UserID / BlogHead that already exist.
Reception    : DoctorReceptionStaff.UserID = s2.reception  (Dev ACL proof)
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

DECLARE @Marker NVARCHAR(40) = N'S2-TESTED';
DECLARE @SeedDate DATETIME = '20260918';

IF OBJECT_ID(N'dbo.HumanSystemMaster', N'U') IS NULL
BEGIN
    RAISERROR('HumanSystemMaster missing. Stop.', 16, 1);
    RETURN;
END

BEGIN TRAN;

-- ---------------------------------------------------------------------------
-- Care categories (PAT-09 GET /api/PatientPortal/CareCategories)
-- ---------------------------------------------------------------------------
;WITH cats AS (
    SELECT * FROM (VALUES
        (N'Mind', N'S2-TESTED mind / emotions'),
        (N'Head', N'S2-TESTED head / headache'),
        (N'Eyes', N'S2-TESTED eyes'),
        (N'Ears', N'S2-TESTED ears'),
        (N'Nose', N'S2-TESTED nose / coryza'),
        (N'Throat', N'S2-TESTED throat'),
        (N'Chest', N'S2-TESTED chest / respiration'),
        (N'Stomach', N'S2-TESTED stomach / digestion'),
        (N'Abdomen', N'S2-TESTED abdomen'),
        (N'Skin', N'S2-TESTED skin'),
        (N'Sleep', N'S2-TESTED sleep'),
        (N'Fever', N'S2-TESTED fever / chill')
    ) AS v(HumanSystemName, Description)
)
INSERT INTO dbo.HumanSystemMaster (HumanSystemName, Description)
SELECT c.HumanSystemName, c.Description
FROM cats c
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.HumanSystemMaster h
    WHERE h.HumanSystemName = c.HumanSystemName
);

DECLARE @CatCount INT = (SELECT COUNT(*) FROM dbo.HumanSystemMaster);
PRINT CONCAT('HumanSystemMaster care categories now = ', @CatCount);

-- ---------------------------------------------------------------------------
-- Dev reception staff for DoctorOnly live 403 (login UserID s2.reception)
-- Password is plaintext; ReceptionStaffPasswordHelper accepts plaintext.
-- ---------------------------------------------------------------------------
DECLARE @DoctorId INT;
SELECT TOP 1 @DoctorId = d.DoctorId
FROM dbo.Doctor d
WHERE ISNULL(d.DeleteStatus, 0) = 0
  AND d.DirectoryVisible = 1
  AND d.VerificationStatus = N'Verified'
ORDER BY CASE WHEN d.DoctorId = 3 THEN 0 ELSE 1 END, d.DoctorId;

IF @DoctorId IS NULL
BEGIN
    RAISERROR('Need a Verified directory doctor for reception seed. Stop.', 16, 1);
    ROLLBACK TRAN;
    RETURN;
END

IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM dbo.DoctorReceptionStaff r
        WHERE r.UserID = N's2.reception' AND ISNULL(r.DeleteStatus, 0) = 0
   )
BEGIN
    INSERT INTO dbo.DoctorReceptionStaff (
        DoctorID, UserID, Password, FullName, Address, ContactNumber, EmailId,
        Country, State, City, EnteredBy, EnteredDate, DeleteStatus
    )
    VALUES (
        @DoctorId,
        N's2.reception',
        N'S2Test@123',
        N'S2 Reception',
        N'S2-TESTED',
        N'9000000099',
        N's2.reception@homeocentrum.dev',
        N'India', N'Maharashtra', N'Pune',
        NULL, @SeedDate, 0
    );
    PRINT 'INSERTED DoctorReceptionStaff UserID=s2.reception';
END
ELSE
    PRINT 'SKIP DoctorReceptionStaff s2.reception already present';

-- ---------------------------------------------------------------------------
-- Article body for GET /api/Public/Articles/{id}
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.BlogDetails', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.BlogDetails WHERE BlogHead = N'S2-TESTED care article')
BEGIN
    INSERT INTO dbo.BlogDetails (
        BlogHead, BlogSubHead, BlogDate, BlogImage1, BlogImage2, BlogDetails,
        IsActive, EnteredBy, EnteredDate
    )
    VALUES (
        N'S2-TESTED care article',
        N'Homeopathy care category sample',
        @SeedDate,
        NULL,
        NULL,
        N'<p>S2-TESTED article body for GET /api/Public/Articles/{id}.</p>',
        1,
        NULL,
        @SeedDate
    );
    PRINT 'INSERTED BlogDetails S2-TESTED care article';
END
ELSE
    PRINT 'SKIP BlogDetails S2-TESTED care article already present';

COMMIT TRAN;

DECLARE @CatCount2 INT = (SELECT COUNT(*) FROM dbo.HumanSystemMaster);
DECLARE @RecCount INT = 0;
DECLARE @BlogCount INT = 0;
IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NOT NULL
    SET @RecCount = (SELECT COUNT(*) FROM dbo.DoctorReceptionStaff WHERE UserID = N's2.reception' AND ISNULL(DeleteStatus,0)=0);
IF OBJECT_ID(N'dbo.BlogDetails', N'U') IS NOT NULL
    SET @BlogCount = (SELECT COUNT(*) FROM dbo.BlogDetails WHERE ISNULL(IsActive,0)=1);

PRINT '----------------------------------------';
PRINT CONCAT('CareCategories rows = ', @CatCount2);
PRINT CONCAT('Reception s2.reception rows = ', @RecCount);
PRINT CONCAT('Active BlogDetails = ', @BlogCount);
PRINT '08_CARE_CATEGORIES_RECEPTION_ARTICLES OK';

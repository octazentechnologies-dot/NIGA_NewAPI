/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M06_REC_02_01.sql
Purpose      : REC-02.01 — Profile/Me role=Reception uses dbo.DoctorReceptionStaff.
               UserMaster is the doctor/admin login. It does not hold reception
               profile rows, so it does not suffice. No new table is created.
Use          : HomeoCentrum_Dev first, then UAT. Idempotent. Read-only.
Prerequisites: dbo.DoctorReceptionStaff exists (DOC-09).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.DoctorReceptionStaff', N'U') IS NULL
BEGIN
    RAISERROR('dbo.DoctorReceptionStaff is missing. Do not create a new profile table.', 16, 1);
    RETURN;
END
GO

IF OBJECT_ID(N'dbo.ReceptionProfile', N'U') IS NOT NULL
    RAISERROR('dbo.ReceptionProfile must not exist. REC-02.01 reuses DoctorReceptionStaff.', 16, 1);
GO

PRINT 'M06_REC_02_01.sql: no new table. Reception Profile/Me reads DoctorReceptionStaff.';

SELECT c.name AS ColumnName
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.DoctorReceptionStaff')
  AND c.name IN (
        N'ReceptionStaffID', N'DoctorID', N'UserID', N'FullName',
        N'ContactNumber', N'EmailId', N'Address', N'City', N'State', N'Country', N'IsActive'
  )
ORDER BY c.column_id;

SELECT TOP 5
    ReceptionStaffID,
    DoctorID,
    UserID,
    FullName,
    ContactNumber,
    EmailId,
    City,
    IsActive,
    DeleteStatus
FROM dbo.DoctorReceptionStaff
WHERE ISNULL(DeleteStatus, 0) = 0
ORDER BY ReceptionStaffID;
GO

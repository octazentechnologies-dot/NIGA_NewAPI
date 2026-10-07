/*
  14 - Objects and data fixes that no earlier script carries.

    IX_UserMaster_UserName      unique index used by login lookups (exists on the local database only).
                                Stops with an error, changing nothing, when duplicate user names exist.
    FamilyRelationMaster        Child, Parent and Sibling, used by PatientFamilyMember.Relation text
                                written by older UI builds. GET /api/Family resolves RelationId by name.
    "string" placeholders       Swagger default values saved into Patient and CaseEntryDetails. Cleared
                                to NULL so APIs return real data or null instead of the word "string".

  Safe to run more than once.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.UserMaster') AND name = N'IX_UserMaster_UserName'
)
BEGIN
    IF EXISTS (SELECT UserName FROM dbo.UserMaster GROUP BY UserName HAVING COUNT(*) > 1)
        RAISERROR('UserMaster has duplicate UserName values. Resolve them before creating IX_UserMaster_UserName.', 16, 1);
    ELSE
        CREATE UNIQUE NONCLUSTERED INDEX IX_UserMaster_UserName ON dbo.UserMaster (UserName);
END
GO

DECLARE @Now datetime = GETDATE();
DECLARE @Relations TABLE (RelationName nvarchar(100) NOT NULL, SortOrder int NOT NULL);
INSERT INTO @Relations (RelationName, SortOrder) VALUES (N'Child', 170), (N'Parent', 180), (N'Sibling', 190);

INSERT INTO dbo.FamilyRelationMaster (RelationName, SortOrder, DeleteStatus, EnteredBy, EnteredDate)
SELECT r.RelationName, r.SortOrder, 0, N'S5-Week5', @Now
FROM @Relations r
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.FamilyRelationMaster m
    WHERE LTRIM(RTRIM(m.RelationName)) = r.RelationName
);
GO

UPDATE dbo.Patient SET Email = NULL WHERE LTRIM(RTRIM(Email)) = N'string';
UPDATE dbo.Patient SET PhoneNo = NULL WHERE LTRIM(RTRIM(PhoneNo)) = N'string';
UPDATE dbo.Patient SET AddressLine1 = NULL WHERE LTRIM(RTRIM(AddressLine1)) = N'string';
UPDATE dbo.Patient
SET Address = NULLIF(LTRIM(RTRIM(CONCAT_WS(N', ', AddressLine1, AddressLine2, Landmark))), N'')
WHERE LTRIM(RTRIM(Address)) = N'string';
UPDATE dbo.CaseEntryDetails SET RefBy = NULL WHERE LTRIM(RTRIM(RefBy)) = N'string';
GO

PRINT '14_Required_Extras: done';

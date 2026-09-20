/*
================================================================================
Author       : Tufan Powar
Created      : 20-09-2026
Script       : 12_DEV_Seed_Role_Users.sql
Purpose      : Dev UserMaster logins for added roles that have no user yet
               (Account, PharmacyPartner). Password hash copied from
               tufanpowar001@gmail.com (plaintext 123456).
Use          : Dev HomeoCentrum_Dev only. Idempotent.
Logins       : s2.account / 123456    s2.pharmacy / 123456
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NULL OR OBJECT_ID(N'dbo.RoleMaster', N'U') IS NULL
BEGIN
    RAISERROR('UserMaster / RoleMaster missing.', 16, 1);
    RETURN;
END

DECLARE @Pwd NVARCHAR(500) = (
    SELECT TOP 1 UserPassword
    FROM dbo.UserMaster
    WHERE UserName = N'tufanpowar001@gmail.com'
      AND ISNULL(DeleteStatus, 0) = 0
);

IF @Pwd IS NULL OR LTRIM(RTRIM(@Pwd)) = N''
    SET @Pwd = N'PBKDF2$v1$100000$uszumO1aPL3it0x1tm/FcA==$hvFLmFwfXrC7i3Id07HFiH9ah4WiIAdjzYPyQnArLxQ='; -- 123456

DECLARE @HasFirstName BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'FirstName') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasLastName BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'LastName') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasEmail BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'EmailId') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasMobile BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'MobileNo') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasCountry BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'CountryId') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasState BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'StateId') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasActivated BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'IsUserActivated') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasEnteredBy BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'EnteredBy') IS NULL THEN 0 ELSE 1 END;
DECLARE @HasEnteredDate BIT = CASE WHEN COL_LENGTH(N'dbo.UserMaster', N'EnteredDate') IS NULL THEN 0 ELSE 1 END;

DECLARE @Roles TABLE
(
    RoleName NVARCHAR(50) PRIMARY KEY,
    UserName NVARCHAR(200) NOT NULL,
    EmailId NVARCHAR(200) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    MobileNo NVARCHAR(20) NOT NULL
);

INSERT INTO @Roles (RoleName, UserName, EmailId, FirstName, LastName, MobileNo) VALUES
    (N'Account',          N's2.account',  N's2.account@homeocentrum.dev',  N'S2', N'Account',  N'9000000101'),
    (N'PharmacyPartner',  N's2.pharmacy', N's2.pharmacy@homeocentrum.dev', N'S2', N'Pharmacy', N'9000000102');

DECLARE @RoleName NVARCHAR(50), @UserName NVARCHAR(200), @Email NVARCHAR(200),
        @First NVARCHAR(100), @Last NVARCHAR(100), @Mobile NVARCHAR(20), @RoleId INT;
DECLARE @Sql NVARCHAR(MAX);
DECLARE @Vals NVARCHAR(MAX);

DECLARE role_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT RoleName, UserName, EmailId, FirstName, LastName, MobileNo FROM @Roles;

OPEN role_cursor;
FETCH NEXT FROM role_cursor INTO @RoleName, @UserName, @Email, @First, @Last, @Mobile;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @RoleId = (
        SELECT TOP 1 RoleId FROM dbo.RoleMaster
        WHERE RoleName = @RoleName AND ISNULL(DeleteStatus, 0) = 0
    );

    IF @RoleId IS NULL
        PRINT CONCAT('SKIP role missing: ', @RoleName);
    ELSE IF EXISTS (
        SELECT 1 FROM dbo.UserMaster
        WHERE ISNULL(DeleteStatus, 0) = 0
          AND (UserName = @UserName OR (@HasEmail = 1 AND EmailId = @Email))
    )
        PRINT CONCAT('SKIP user already present: ', @UserName);
    ELSE
    BEGIN
        SET @Sql = N'INSERT INTO dbo.UserMaster (UserName, UserPassword, UserStatus, DeleteStatus, RoleId';
        SET @Vals = N' VALUES (@pUser, @pPwd, 1, 0, @pRole';

        IF @HasMobile = 1 BEGIN SET @Sql += N', MobileNo'; SET @Vals += N', @pMobile'; END
        IF @HasEmail = 1 BEGIN SET @Sql += N', EmailId'; SET @Vals += N', @pEmail'; END
        IF @HasFirstName = 1 BEGIN SET @Sql += N', FirstName'; SET @Vals += N', @pFirst'; END
        IF @HasLastName = 1 BEGIN SET @Sql += N', LastName'; SET @Vals += N', @pLast'; END
        IF @HasCountry = 1 BEGIN SET @Sql += N', CountryId'; SET @Vals += N', 78'; END
        IF @HasState = 1 BEGIN SET @Sql += N', StateId'; SET @Vals += N', 14'; END
        IF @HasActivated = 1 BEGIN SET @Sql += N', IsUserActivated'; SET @Vals += N', 1'; END
        IF @HasEnteredBy = 1 BEGIN SET @Sql += N', EnteredBy'; SET @Vals += N', N''S2-ROLE'''; END
        IF @HasEnteredDate = 1 BEGIN SET @Sql += N', EnteredDate'; SET @Vals += N', GETDATE()'; END

        SET @Sql += N')' + @Vals + N')';

        EXEC sp_executesql @Sql,
            N'@pUser NVARCHAR(200), @pPwd NVARCHAR(500), @pRole INT, @pMobile NVARCHAR(20), @pEmail NVARCHAR(200), @pFirst NVARCHAR(100), @pLast NVARCHAR(100)',
            @pUser = @UserName, @pPwd = @Pwd, @pRole = @RoleId,
            @pMobile = @Mobile, @pEmail = @Email, @pFirst = @First, @pLast = @Last;

        PRINT CONCAT('INSERTED UserMaster ', @UserName, ' role=', @RoleName);
    END

    FETCH NEXT FROM role_cursor INTO @RoleName, @UserName, @Email, @First, @Last, @Mobile;
END

CLOSE role_cursor;
DEALLOCATE role_cursor;

PRINT '12_DEV_Seed_Role_Users.sql completed.';
PRINT 'Logins: s2.account / 123456   s2.pharmacy / 123456';
GO

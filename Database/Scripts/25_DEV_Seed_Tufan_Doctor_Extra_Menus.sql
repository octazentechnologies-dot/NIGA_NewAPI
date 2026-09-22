/*
================================================================================
Author       : Tufan Powar
Created      : 22-09-2026
Script       : 25_DEV_Seed_Tufan_Doctor_Extra_Menus.sql
Purpose      : Give Tufan_Doctor every available MenuMaster item other Doctor-role
               logins (NIGA HOMEOPATHY, testdoctor, …) do not have. Stored in
               UserDetails (not RoleDetails) so RoleId=3 stays unchanged.
               GetMenuByRole unions UserDetails.IsView for that UserId.
               No separate SPA hide/show implementation — existing More dropdown
               shows whatever GetMenuByRole returns beyond the 5 clinic menus.
Use          : HomeoCentrum_Dev after 19/24. Idempotent.
Do not run   : Production.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NULL
   OR OBJECT_ID(N'dbo.UserDetails', N'U') IS NULL
   OR OBJECT_ID(N'dbo.MenuMaster', N'U') IS NULL
BEGIN
    RAISERROR('UserMaster / UserDetails / MenuMaster missing.', 16, 1);
    RETURN;
END

DECLARE @Uid BIGINT = (
    SELECT TOP 1 UserId
    FROM dbo.UserMaster
    WHERE UserName = N'Tufan_Doctor' AND ISNULL(DeleteStatus, 0) = 0
);

IF @Uid IS NULL
BEGIN
    RAISERROR('Tufan_Doctor UserMaster row missing. Run 15/23 first.', 16, 1);
    RETURN;
END

INSERT INTO dbo.UserDetails (UserId, MenuId, IsView, IsAdd, IsModify, IsDelete, FirmId)
SELECT @Uid, m.MenuId, 1, 1, 1, 1, NULL
FROM dbo.MenuMaster m
WHERE ISNULL(m.DeleteStatus, 0) = 0
  AND ISNULL(m.ShowInMainMenu, 1) = 1
  AND NOT EXISTS (
        SELECT 1 FROM dbo.UserDetails ud
        WHERE ud.UserId = @Uid AND ud.MenuId = m.MenuId
  );

UPDATE ud
SET IsView = 1, IsAdd = 1, IsModify = 1, IsDelete = 1
FROM dbo.UserDetails ud
INNER JOIN dbo.MenuMaster m ON m.MenuId = ud.MenuId
WHERE ud.UserId = @Uid
  AND ISNULL(m.DeleteStatus, 0) = 0
  AND ISNULL(m.ShowInMainMenu, 1) = 1;

PRINT CONCAT('Tufan_Doctor all available menus UserId=', @Uid, ' rows=', @@ROWCOUNT);
SELECT ud.MenuId, m.MenuName, m.MenuUrl, ud.IsView, ud.IsAdd, ud.IsModify, ud.IsDelete
FROM dbo.UserDetails ud
INNER JOIN dbo.MenuMaster m ON m.MenuId = ud.MenuId
WHERE ud.UserId = @Uid
ORDER BY m.SeqNo, m.MenuName;
